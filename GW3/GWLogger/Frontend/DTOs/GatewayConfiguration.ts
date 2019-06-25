/**
 * DTO of GWLogger.Backend.Controllers.XmlGatewayConfig
 */
interface XmlGatewayConfig
{
    Type: GatewayConfigurationType;
    Name: string;
    LocalAddressSideA: string;
    RemoteAddressSideA: string;
    LocalAddressSideB: string;
    RemoteAddressSideB: string;
    Security: ConfigSecurity;
}

enum GatewayConfigurationType
{
    UNIDIRECTIONAL = 0,
    BIDIRECTIONAL = 1,
}

/**
 * DTO of GWLogger.Backend.Controllers.class
 */
interface ConfigSecurity
{
    Groups: ConfigSecurityGroup[];
    RulesSideA: ConfigSecurityRule[];
    RulesSideB: ConfigSecurityRule[];
}

/**
 * DTO of GWLogger.Backend.Controllers.ConfigSecurityGroup
 */
interface ConfigSecurityGroup
{
    Filters: SecurityFilter[];
    Name: string;
}

/**
 * DTO of GWLogger.Backend.Controllers.SecurityFilter
 */
interface SecurityFilter
{
    $type: string;
    Name?: string;
    IP?: string;
}

/**
 * DTO of GWLogger.Backend.Controllers.ConfigSecurityRule
 */
interface ConfigSecurityRule
{
    Filter: SecurityFilter;
    Channel: string;
    Access: string;
}
