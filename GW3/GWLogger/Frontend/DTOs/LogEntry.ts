interface LogEntry
{
    Date: number,
    Type: number,
    Level: number,
    Message: string,
    Remote: string,
    Position: number,
    CurrentFile: string,
    Details: { [s: string]: string; }
}