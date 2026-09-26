using NUnitLite;
public static class CooRunProgram
{
    public static int Main(string[] args) => new AutoRun(typeof(CooRunProgram).Assembly).Execute(args);
}
