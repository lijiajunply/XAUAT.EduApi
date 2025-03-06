using System.Text.RegularExpressions;

namespace XAUAT.EduApi.DataModels;

public class SemesterModel
{
    public string SemesterId { get; set; } = "";
    public string SemesterName { get; set; } = "";
}

[Serializable]
public class SemesterItem
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
}

[Serializable]
public class SemesterResult
{
    public List<SemesterItem> Data { get; } = [];

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
}