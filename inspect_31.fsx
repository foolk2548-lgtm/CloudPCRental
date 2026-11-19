open System.IO
open System.Reflection.Metadata
open System.Reflection.PortableExecutable

let dllPath = @"CloudPCRental\bin\Debug\net8.0\CloudPCRental.dll"
use stream = File.OpenRead(dllPath)
use peReader = new PEReader(stream)
let metadataReader = peReader.GetMetadataReader()

for typeDefHandle in metadataReader.TypeDefinitions do
    let typeDef = metadataReader.GetTypeDefinition(typeDefHandle)
    let name = metadataReader.GetString(typeDef.Name)
    let ns = metadataReader.GetString(typeDef.Namespace)
    let fullName = if ns = "" then name else ns + "." + name
    if name.Contains("31") || fullName.Contains("31") then
        // find enclosing type if nested
        let isNested = typeDef.IsNested
        if isNested then
            let declaringHandle = typeDef.GetDeclaringType()
            let declaringDef = metadataReader.GetTypeDefinition(declaringHandle)
            let decName = metadataReader.GetString(declaringDef.Name)
            let decNs = metadataReader.GetString(declaringDef.Namespace)
            printfn "Nested: %s in %s.%s" name decNs decName
        else
            printfn "Top: %s" fullName
