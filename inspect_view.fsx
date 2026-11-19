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
    if fullName.Contains("Views_Admin_SupportTickets") then
        printfn "Found view: %s" fullName
        // list nested types
        for nestedHandle in typeDef.GetNestedTypes() do
            let nested = metadataReader.GetTypeDefinition(nestedHandle)
            let nName = metadataReader.GetString(nested.Name)
            printfn "  Nested: %s" nName
