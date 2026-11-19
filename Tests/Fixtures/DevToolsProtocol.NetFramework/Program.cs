using System;
using System.IO;
using System.Linq;
using Nazor.DevTools.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

var assembly = typeof(TreeSnapshot).Assembly;
if (assembly.GetType("Nazor.DevTools.Protocol.DevToolsPeer") != null || assembly.GetReferencedAssemblies().Any(a => a.Name == "System.Text.Json" || a.Name.StartsWith("Nazor.")))
    throw new Exception("The .NET Framework contract asset must contain only plain contracts.");
var snapshot = JsonConvert.DeserializeObject<TreeSnapshot>(File.ReadAllText(args[0]));
if (snapshot == null || snapshot.Elements[0].Text != "Caffè 漢字 🙂" || snapshot.Capture.SessionId != "session-instance")
    throw new Exception("The .NET Framework JSON client lost contract data.");
File.WriteAllText(args[1], JsonConvert.SerializeObject(snapshot, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
Console.WriteLine("PASS: .NET Framework 4.7.2 uses the netstandard2.0 DTO asset with its existing JSON serializer.");
