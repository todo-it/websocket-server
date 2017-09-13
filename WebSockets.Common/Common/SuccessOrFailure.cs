namespace WebSockets.Common.Common
{
    public class SuccessOrFailure<T>
    {
        public bool IsSuccess {get; }
        public T Value { get; }

        private SuccessOrFailure(T value, bool success)
        {
            Value = value;
            IsSuccess = success;
        }
        
        public static SuccessOrFailure<T> CreateSuccess(T value)
        {
            return new SuccessOrFailure<T>(value, true);
        }

        public static SuccessOrFailure<T> CreateFailure(T value)
        {
            return new SuccessOrFailure<T>(value, false);
        }
    }
}
