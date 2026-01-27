# Cookie认证异常处理实现计划

## 1. 创建异常类
- 创建 `UnAuthenticationError` 异常类，用于标识认证失败情况

## 2. 修改服务方法
在所有使用Cookie的服务方法中添加检查：
- `ProgramService.GetAllTrainProgram`：修改返回空列表为抛出异常
- `CookieCodeService.GetCode`：添加内容检查
- `ScoreService.ParseSemesterAsync`：添加内容检查
- `ScoreService.CrawlScores`：添加"登入页面"检查
- `CourseService.GetCoursesAsync`：修改抛出的异常类型为 `UnAuthenticationError`
- `ExamService.GetExamArrangementAsync`：修改返回错误对象为抛出异常
- `InfoController.GetCompletion`：在控制器中添加检查

## 3. 修改控制器
在所有使用Cookie的控制器方法中添加try-catch块：
- `ProgramController.GetAllTrainProgram`
- `ProgramController.GetAllTrainPrograms`
- `ScoreController.GetThisSemester`
- `ScoreController`中的其他方法（如果有）
- `CourseController.GetCourse`
- `InfoController.GetCompletion`

## 4. 实现步骤
1. 创建 `UnAuthenticationError` 异常类
2. 修改各个服务方法，添加认证检查
3. 修改控制器方法，添加异常捕获和401返回
4. 测试所有修改后的方法

## 5. 预期效果
- 当Cookie失效或需要重新登录时，系统会抛出 `UnAuthenticationError` 异常
- 控制器捕获到该异常后，返回401状态码
- 前端可以根据401状态码进行相应处理，如跳转到登录页面

## 6. 代码规范
- 遵循项目现有的命名规范
- 保持代码的可读性和可维护性
- 添加必要的注释
- 确保所有修改都经过测试