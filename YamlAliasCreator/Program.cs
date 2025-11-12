using System; using System.Collections.Generic; using System.Linq; using Mono.Cecil; using Mono.Cecil.Cil;

namespace YamlAliasCreator
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"YamlAliasCreator for {args[0]}");
            string dll = args[0];
            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(dll, new ReaderParameters { ReadWrite = true });
            ModuleDefinition module = asm.MainModule;

            foreach (TypeDefinition type in module.Types) ProcessType(type, module);
            asm.Write();
        }

        static void ProcessType(TypeDefinition type, ModuleDefinition module)
        {
            if (!type.IsClass) return;
            foreach (FieldDefinition field in type.Fields.ToList())
            {
                if (field.CustomAttributes.Count == 0) continue;
                List<CustomAttribute> aliasAttrs = field.CustomAttributes.Where(a => a.AttributeType.Name == "DbAliasAttribute").ToList();
                if (aliasAttrs.Count == 0) continue;
                List<string> allAliases = new List<string>();
                foreach (CustomAttribute ca in aliasAttrs)
                {
                    if (ca.ConstructorArguments.Count == 0) continue;
                    if (ca.ConstructorArguments[0].Value is IList<CustomAttributeArgument> arr)
                    {
                        foreach (CustomAttributeArgument elem in arr)
                        {
                            if (elem.Value is string s && !string.IsNullOrWhiteSpace(s)) allAliases.Add(s);
                        }
                    }
                    else if (ca.ConstructorArguments[0].Value is string s1 && !string.IsNullOrWhiteSpace(s1)) allAliases.Add(s1);
                }

                foreach (string alias in allAliases.Distinct())
                {
                    if (type.Properties.Any(p => p.Name == alias)) continue;
                    if (type.Methods.Any(m => m.Name == $"set_{alias}")) continue;
                    MethodDefinition setMethod = new MethodDefinition($"set_{alias}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, module.TypeSystem.Void);
                    ParameterDefinition param = new ParameterDefinition("value", ParameterAttributes.None, field.FieldType);
                    setMethod.Parameters.Add(param);
                    ILProcessor il = setMethod.Body.GetILProcessor();
                    il.Append(il.Create(OpCodes.Ldarg_0));
                    il.Append(il.Create(OpCodes.Ldarg_1));
                    il.Append(il.Create(OpCodes.Stfld, field));
                    il.Append(il.Create(OpCodes.Ret));
                    type.Methods.Add(setMethod);
                    PropertyDefinition prop = new PropertyDefinition(alias, PropertyAttributes.None, field.FieldType) { SetMethod = setMethod };
                    type.Properties.Add(prop);
                }
            }
            foreach (var nested in type.NestedTypes) ProcessType(nested, module);
        }
    }
}
