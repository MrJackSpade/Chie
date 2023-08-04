using Ai.Utils.Extensions;
using Loxifi;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.Services
{
    public static class MacroService
    {
        public static async Task<string> Transform(string input)
        {
            List<string> outputLines = new();

            Dictionary<string, object> macros = new();

            foreach (string line in input.Split(Environment.NewLine))
            {
                //Vicuna prompt short circuit
                if (line.StartsWith("###"))
                {
                    outputLines.Add(line); 
                    continue;
                }

                if (line.StartsWith("#"))
                {
                    await ResolveCommand(line, macros);
                }
                else if (line.Contains("%%"))
                {
                    string nl = line;

                    List<Match> macroMatches = Regex.Matches(line, "%%([a-zA-Z0-9_]+)%%").ToList();

                    foreach (Match m in macroMatches)
                    {
                        string value = macros[m.Groups[1].Value].ToString();
                        nl = nl.Replace(m.Groups[0].Value, value);
                    }

                    outputLines.Add(nl);
                }
                else
                {
                    outputLines.Add(line);
                }
            }

            return string.Join(Environment.NewLine, outputLines.ToArray());
        }

        public static async Task<string> TransformFile(string filePath) => await Transform(File.ReadAllText(filePath));

        private static object GetMemberValue(object source, string memberName, Queue<string> commands)
        {
            Type memberType = source.GetType();

            if (memberType.GetProperty(memberName) is PropertyInfo propertyInfo)
            {
                return propertyInfo.GetValue(source);
            }

            if (memberType.GetField(memberName) is FieldInfo fieldInfo)
            {
                return fieldInfo.GetValue(source);
            }

            MethodInfo matchedMethod = null;

            List<string> parameters = new();

            while (commands.Any())
            {
                parameters.Add(commands.Dequeue());
            }

            foreach (MethodInfo mi in memberType.GetMethods().Where(m => m.Name == memberName))
            {
                if (mi.GetParameters().Length == parameters.Count)
                {
                    matchedMethod = mi;
                    break;
                }
            }

            if (matchedMethod != null)
            {
                List<object> methodParameters = new();
                List<ParameterInfo> matchedParameters = matchedMethod.GetParameters().ToList();
                for (int i = 0; i < matchedParameters.Count; i++)
                {
                    methodParameters.Add(parameters[i].Convert(matchedParameters[i].ParameterType));
                }

                return matchedMethod.Invoke(source, methodParameters.ToArray());
            }

            throw new NotImplementedException();
        }

        private static object? InstantiateClass(string varSource, Queue<string> commands)
        {
            Type toInstantiate = TypeFactory.Default.GetTypeByFullName(varSource);

            List<string> parameters = new();

            while (commands.Any())
            {
                parameters.Add(commands.Dequeue());
            }

            ConstructorInfo countMatchConstuctor = toInstantiate.GetConstructors().Where(c => c.GetParameters().Count() == parameters.Count()).FirstOrDefault() ?? throw new NotImplementedException();

            List<object> parsedParameters = new();
            List<ParameterInfo> expectedParameters = countMatchConstuctor.GetParameters().ToList();

            for (int i = 0; i < expectedParameters.Count; i++)
            {
                parsedParameters.Add(parameters[i].Convert(expectedParameters[i].ParameterType));
            }

            object toReturn = Activator.CreateInstance(toInstantiate, parsedParameters.ToArray());

            return toReturn;
        }

        private static async Task ResolveCommand(string line, Dictionary<string, object> macros)
        {
            Queue<string> commands = new();

            foreach (string c in line.CleanSplit(' '))
            {
                commands.Enqueue(c);
            }

            string thisCommand = commands.Dequeue();

            switch (thisCommand.ToUpper().Trim('#'))
            {
                case "SET":
                    await ResolveSetCommand(commands, macros);
                    break;

                default: throw new NotImplementedException();
            }
        }

        private static async Task<object> ResolvePath(object source, Queue<string> commands)
        {
            string memberPath = commands.Dequeue();

            object root = source;

            Queue<string> path = new();

            foreach (string chunk in memberPath.Split("."))
            {
                path.Enqueue(chunk);
            }

            while (path.Any())
            {
                root = GetMemberValue(root, path.Dequeue(), commands);

                if (root is Task task)
                {
                    await task;

                    root = task.GetType().GetProperty("Result").GetValue(task);
                }
            }

            return root;
        }

        private static async Task ResolveSetCommand(Queue<string> commands, Dictionary<string, object> macros)
        {
            string varName = commands.Dequeue();
            string varSource = commands.Dequeue();
            string sourceName = commands.Dequeue();
            object source;
            switch (varSource.ToUpper())
            {
                case "MACRO":
                    source = macros[sourceName];
                    object value = await ResolvePath(source, commands);
                    macros.Add(varName, value);
                    break;

                case "CLASS":
                    source = InstantiateClass(sourceName, commands);
                    macros.Add(varName, source);
                    break;

                default: throw new NotImplementedException();
            }
        }
    }
}