namespace Nontiwat_7362_L002.ViewModels;

public static class EmailRules
{
    // Conventional mailbox addresses: no whitespace, empty dot segments or invalid domain labels.
    public const string Pattern = @"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$";
    public const string Message = "กรุณากรอกอีเมลให้ถูกต้อง เช่น name@example.com โดยไม่มีช่องว่าง";
}
