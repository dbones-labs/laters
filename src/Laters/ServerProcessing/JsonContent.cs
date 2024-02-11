namespace Laters.ServerProcessing;

using System.Text;
using System.Text.Json;

public class JsonContent : StringContent
{
    public JsonContent(object obj) :
        base(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json")
    { }
}