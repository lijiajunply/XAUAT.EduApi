using System.Runtime.Serialization;

namespace XAUAT.EduApi.Services;

[Serializable]
public class PaymentServiceException : Exception
{
    public PaymentServiceException()
    {
    }

    public PaymentServiceException(string message) : base(message)
    {
    }

    public PaymentServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    // 保留这个构造函数以支持序列化，但添加抑制警告
#pragma warning disable SYSLIB0051 // 类型或成员已过时
    protected PaymentServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051 // 类型或成员已过时
}