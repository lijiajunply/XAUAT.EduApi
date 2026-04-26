using EduApi.Data.Models;

namespace XAUAT.EduApi.Queues;

public sealed record ScoreCrawledEvent(IReadOnlyCollection<ScoreResponse> Scores);
