using Dapper;
using System.Data;

namespace MB.PosSelection.Infrastructure.Persistence.TypeHandlers
{
    public class StringToEnumHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
    {
        public override void SetValue(IDbDataParameter parameter, T value)
        {
            parameter.Value = value.ToString();
        }

        public override T Parse(object value)
        {
            if (value is string s && Enum.TryParse<T>(s, true, out var result))
            {
                return result;
            }
            return default;
        }
    }
}
