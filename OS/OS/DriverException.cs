namespace OS;

public class DriverException : Exception
{
    public DriverException()
    {
        
    }
    
    public DriverException(string message) : base(message)
    {
        
    }
    
    public DriverException(string message, Exception inner) : base(message, inner)
    {
        
    }
}