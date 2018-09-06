interface LogEntry
{
    Date: number,
    Type: number,
    Level: number,
    Message: string,
    Remote: string,
    Position: number,
    Details: { [s: string]: string; }
}