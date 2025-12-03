import http from './axios';

interface Course {
  courseName: string;
  teacher: string;
  time: string;
  location: string;
  [key: string]: any;
}

interface CourseResponse {
  Success: boolean;
  Data: Course[];
  ExpirationTime: string;
  [key: string]: any;
}

const courseService = {
  /**
   * 获取学生课程信息
   * @param studentId 学生ID，多个ID用逗号分隔
   * @returns 课程列表
   */
  getCourses: async (studentId: string): Promise<CourseResponse> => {
    return http.get('/Course', {
      params: { studentId }
    });
  }
};

export default courseService;
