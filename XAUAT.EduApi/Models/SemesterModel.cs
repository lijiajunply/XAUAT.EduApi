using System.Text.RegularExpressions;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Models;

[Serializable]
public class SemesterItem
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";

    public override string ToString()
    {
        return $"{Text}:{Value}";
    }
}

[Serializable]
public class SemesterResult
{
    public List<SemesterItem?> Data { get; } = [];

    public void Parse(string html)
    {
        // 检查是否登录
        if (!Regex.IsMatch(html, "学生成绩"))
        {
            return;
        }

        // 使用正则表达式匹配所有semester选项
        var regex = new Regex("<option value=\"(.*?)\">(.*?)</option");
        var matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            if (match.Groups.Count != 3)
            {
                continue;
            }

            var text = match.Groups[2].Value;
            if (text == "...")
            {
                continue;
            }

            Data.Add(new SemesterItem
            {
                Value = match.Groups[1].Value,
                Text = text
            });
        }
    }

    public SemesterItem ParseNow(string html, IInfoService service)
    {
        // 检查是否登录
        if (!Regex.IsMatch(html, "课表"))
        {
            return new SemesterItem();
        }

        // 使用正则表达式匹配所有semester选项
        var regexString = "<option value=\"(.*)\">(.*)</option>";

        if (service.IsInSchool())
        {
            regexString = "<option selected=\"selected\" value=\"(.*)\">(.*)</option>";
        }

        var regex = new Regex(regexString);
        var matches = regex.Matches(html);

        if (matches.Count == 0) return new SemesterItem();

        var text = matches.First().Groups[2].Value;

        if (text == "" || text[^1] == '3')
        {
            return new SemesterItem()
            {
                Value = "301",
                Text = "2025-2026-1"
            };
        }

        return new SemesterItem()
        {
            Value = matches.First().Groups[1].Value,
            Text = text
        };
    }
}