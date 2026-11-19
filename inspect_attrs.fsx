open System.IO
open System.Reflection.Metadata
open System.Reflection.PortableExecutable

let dllPath = @"CloudPCRental\bin\Debug\net8.0\CloudPCRental.dll"
use stream = File.OpenRead(dllPath)
use peReader = new PEReader(stream)
let metadataReader = peReader.GetMetadataReader()

for customAttrHandle in metadataReader.CustomAttributes do
    let customAttr = metadataReader.GetCustomAttribute(customAttrHandle)
    // check if it's RazorCompiledItemAttribute
    let ctorHandle = customAttr.Constructor
    let typeName = 
        match ctorHandle.Kind with
        | HandleKind.MemberReference ->
            let memberRef = metadataReader.GetMemberReference(ctorHandle |> unbox)
            match memberRef.Parent.Kind with
            | HandleKind.TypeReference ->
                let tr = metadataReader.GetTypeReference(memberRef.Parent |> unbox)
                metadataReader.GetString(tr.Name)
            | _ -> ""
        | _ -> ""
    if typeName = "RazorCompiledItemAttribute" then
        // decode arguments
        let valBytes = metadataReader.GetBlobBytes(customAttr.Value)
        let s = System.Text.Encoding.UTF8.GetString(valBytes)
        // print if contains SupportTickets or b__
        if s.Contains("Support") || s.Contains("b__") || s.Contains("31") then
            printfn "Attribute: %s" s
