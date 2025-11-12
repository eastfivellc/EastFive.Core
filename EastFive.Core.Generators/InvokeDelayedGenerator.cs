using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace EastFive.Core.Generators
{
    [Generator]
    public class InvokeDelayedGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var source = GenerateInvokeDelayedOverloads();
            context.AddSource("DiscriminatedFunctions.InvokeDelayed.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private string GenerateInvokeDelayedOverloads()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine("namespace EastFive.Functional");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class DiscriminatedFunctions");
            sb.AppendLine("    {");

            // Generate overloads for different combinations
            // Format: (numPrimes, numDelayed)
            var combinations = new[]
            {
                // Common patterns
                (0, 1), (0, 2), (0, 3), (0, 4),
                (1, 1), (1, 2), (1, 3), (1, 4),
                (2, 1), (2, 2), (2, 3), // (2, 4) already exists
                (3, 1), (3, 2), (3, 3), (3, 4),
            };

            foreach (var (numPrimes, numDelayed) in combinations)
            {
                sb.AppendLine(GenerateSingleOverload(numPrimes, numDelayed));
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateSingleOverload(int numPrimes, int numDelayed)
        {
            var sb = new StringBuilder();

            // Build generic type parameters
            var typeParams = new StringBuilder();
            for (int i = 1; i <= numPrimes; i++)
                typeParams.Append($"TPrime{i}, ");
            for (int i = 1; i <= numDelayed; i++)
                typeParams.Append($"TDelayed{i}, ");
            typeParams.Append("TResult");

            // Build func signature (input parameters)
            var funcParams = new StringBuilder();
            for (int i = 1; i <= numPrimes; i++)
                funcParams.Append($"TPrime{i}, ");
            for (int i = 1; i <= numDelayed; i++)
                funcParams.Append($"TDelayed{i}, ");
            funcParams.Append("Task<TResult>");

            // Build return type parameters
            var returnParams = new StringBuilder();
            for (int i = 1; i <= numPrimes; i++)
                returnParams.Append($"TPrime{i}, ");
            returnParams.Append("Task<TResult>");

            // Build method parameters
            var methodParams = new StringBuilder();
            for (int i = 1; i <= numDelayed; i++)
            {
                if (i > 1) methodParams.Append(",\n");
                methodParams.Append($"            InvokeDelayedAsyncCallback<TDelayed{i}, TResult> delayed{i}Callback");
            }

            // Build lambda parameters
            var lambdaParams = string.Join(", ", GenerateList("prime", numPrimes));

            // Generate task completion sources
            var taskSources = new StringBuilder();
            for (int i = 1; i <= numDelayed; i++)
            {
                taskSources.AppendLine($"                var delayed{i}TaskSource = new TaskCompletionSource<(bool proceeded, TDelayed{i} value)>();");
            }

            // Generate RunDelayedCallback calls
            var runCallbacks = new StringBuilder();
            for (int i = 1; i <= numDelayed; i++)
            {
                runCallbacks.AppendLine($"                var task{i} = RunDelayedCallback(delayed{i}Callback, delayed{i}TaskSource, resultTaskSource);");
            }

            // Generate Task.WhenAll parameters
            var whenAllParams = string.Join(",\n                    ", 
                GenerateList("delayed", numDelayed, "TaskSource.Task"));

            // Generate delayed result retrievals
            var delayedResults = new StringBuilder();
            for (int i = 1; i <= numDelayed; i++)
            {
                delayedResults.AppendLine($"                var delayed{i}Result = await delayed{i}TaskSource.Task;");
            }

            // Generate func call parameters
            var funcCallParams = new StringBuilder();
            for (int i = 1; i <= numPrimes; i++)
                funcCallParams.Append($"prime{i}, ");
            for (int i = 1; i <= numDelayed; i++)
            {
                funcCallParams.Append($"delayed{i}Result.value");
                if (i < numDelayed) funcCallParams.Append(", ");
            }

            // Generate the method
            sb.AppendLine($"        public static Func<{returnParams}> InvokeDelayed<{typeParams}>(");
            sb.AppendLine($"            this Func<{funcParams}> func,");
            sb.Append(methodParams.ToString());
            sb.AppendLine(")");
            sb.AppendLine("        {");
            sb.AppendLine(taskSources.ToString().TrimEnd());
            sb.AppendLine("                ");
            sb.AppendLine("                var resultTaskSource = new TaskCompletionSource<TResult>();");
            sb.AppendLine();
            sb.AppendLine("                // Start all callbacks in parallel using the helper method");
            sb.AppendLine(runCallbacks.ToString().TrimEnd());
            sb.AppendLine("                ");

            if (numPrimes == 0)
            {
                sb.AppendLine("            return async () =>");
            }
            else
            {
                sb.AppendLine($"            return async ({lambdaParams}) =>");
            }
            
            sb.AppendLine("            {");
            sb.AppendLine("                // Race between short-circuit result and all delayed values being ready");
            sb.AppendLine("                var allDelayedValuesTask = Task.WhenAll(");
            sb.AppendLine($"                    {whenAllParams});");
            sb.AppendLine();
            sb.AppendLine("                // Wait for either a short-circuit or all delayed values");
            sb.AppendLine("                await Task.WhenAny(resultTaskSource.Task, allDelayedValuesTask);");
            sb.AppendLine();
            sb.AppendLine("                // If any callback short-circuited, return that result immediately");
            sb.AppendLine("                if (resultTaskSource.Task.IsCompleted)");
            sb.AppendLine("                    return await resultTaskSource.Task;");
            sb.AppendLine();
            sb.AppendLine("                // All callbacks proceeded - get their values");
            sb.AppendLine(delayedResults.ToString().TrimEnd());
            sb.AppendLine();
            sb.AppendLine("                // Call the original function and set the result");
            sb.AppendLine($"                var finalResult = await func({funcCallParams});");
            sb.AppendLine("                ");
            sb.AppendLine("                resultTaskSource.SetResult(finalResult);");
            sb.AppendLine("                return finalResult;");
            sb.AppendLine("            };");
            sb.AppendLine("        }");

            return sb.ToString();
        }

        private string[] GenerateList(string prefix, int count, string suffix = "")
        {
            var list = new string[count];
            for (int i = 0; i < count; i++)
                list[i] = $"{prefix}{i + 1}{suffix}";
            return list;
        }
    }
}
