using System.Text;

namespace SharpDb;

public readonly struct DbParameter
{
    public readonly string Name;
    public readonly object? Value;

    public DbParameter(string name, object? value)
    {
        if (name is not null && name.StartsWith('@'))
            name = name[1..];
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(Resources.Text_Error_QueryParameter_EmptyName, nameof(name));
        if (name.StartsWith('@'))
            throw new ArgumentException(Resources.Text_Error_QueryParameter_TooManyAtSigns, nameof(name));
        Name = name;
        Value = value;
    }

    public static implicit operator DbParameter((string, object?) parameter) => new(parameter.Item1, parameter.Item2);

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append('@');
        sb.Append(Name);
        sb.Append(" = ");

        if (Value is null)
            sb.Append("null");
        else if (Value is string strValue)
            sb.Append('\'').Append(strValue.Replace("'", "''")).Append('\'');
        else
            sb.Append(Value.ToString());

        return sb.ToString();
    }
}
