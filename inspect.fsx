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
    if fullName.Contains("Support") || fullName.Contains("31") || fullName.Contains("Attribute") || fullName.Contains("ExecuteAsync") then
        printfn "%s" fullName
