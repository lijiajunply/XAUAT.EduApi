import http from './axios';

interface SemesterItem {
  name: string;
  value: string;
  [key: string]: any;
}

interface SemesterResult {
  success: boolean;
  semesters: SemesterItem[];
  [key: string]: any;
}

interface ScoreResponse {
  courseName: string;
  score: string;
  semester: string;
  credit: number;
  [key: string]: any;
}

const scoreService = {
  /**
   * 解析学期数据
   * @param studentId 学生ID，多个ID用逗号分隔
   * @returns 学期数据列表
   */
  parseSemester: async (studentId?: string): Promise<SemesterResult> => {
    return http.get('/Score/Semester', {
      params: { studentId }
    });
  },

  /**
   * 获取当前学期
   * @returns 当前学期信息
   */
  getThisSemester: async (): Promise<SemesterItem> => {
    return http.get('/Score/ThisSemester');
  },

  /**
   * 获取学生成绩
   * @param studentId 学生ID，多个ID用逗号分隔
   * @param semester 学期代码，如2024-2025-2
   * @returns 成绩列表
   */
  getScores: async (studentId: string, semester: string): Promise<ScoreResponse[]> => {
    return http.get('/Score', {
      params: { studentId, semester }
    });
  }
};

export default scoreService;
