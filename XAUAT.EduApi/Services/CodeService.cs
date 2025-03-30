using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace XAUAT.EduApi.Services;

public class CodeService : ICodeService
{
    public object Encode(object loginParams)
    {
        ArgumentNullException.ThrowIfNull(loginParams);

        // 将传入的对象转换为具体的类型
        var jsonString = JsonSerializer.Serialize(loginParams);
        var parameters = JsonNode.Parse(jsonString);
        if (parameters == null)
        {
            throw new ArgumentException("Invalid login parameters", nameof(loginParams));
        }

        // 计算 SHA1
        var encPassword = CalculateSHA1($"{parameters["salt"]}-{parameters["password"]}");

        // 创建返回对象
        var result = new
        {
            username = parameters["username"],
            password = encPassword,
            captcha = ""
        };

        return result;
    }

    private static string CalculateSHA1(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA1.HashData(inputBytes);

        // 将字节数组转换为十六进制字符串
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}