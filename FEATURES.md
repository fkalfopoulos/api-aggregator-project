# JWT Authentication & Performance Monitoring Features

This document describes the optional features that have been implemented in the API Aggregator Service.

## ?? Table of Contents

- [JWT Bearer Authentication](#jwt-bearer-authentication)
- [Performance Monitoring Background Service](#performance-monitoring-background-service)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Testing](#testing)

## ?? JWT Bearer Authentication

The API Aggregator now supports JWT (JSON Web Token) bearer authentication to secure all API endpoints.

### Features

- **Token-based Authentication**: Secure API access using industry-standard JWT tokens
- **Role-based Authorization**: Support for multiple user roles (Admin, User, ReadOnly)
- **Password Hashing**: Passwords are securely hashed using BCrypt
- **Configurable Token Expiration**: Set custom token lifetime
- **Demo Users**: Pre-configured users for testing and development

### Architecture

```
????????????????????????????????????????????????????
?         AuthenticationController                  ?
?  POST /api/authentication/login                  ?
?  GET  /api/authentication/me                     ?
????????????????????????????????????????????????????
                    ?
                    ?
????????????????????????????????????????????????????
?         AuthenticationService                     ?
?  - Validates credentials                         ?
?  - Manages demo user database                    ?
????????????????????????????????????????????????????
                    ?
                    ?
????????????????????????????????????????????????????
?              TokenService                         ?
?  - Generates JWT tokens                          ?
?  - Configures token claims                       ?
????????????????????????????????????????????????????
```

### Demo Users

Three pre-configured users are available for testing:

| Username | Password    | Roles           | Description              |
|----------|-------------|-----------------|--------------------------|
| admin    | admin123    | Admin, User     | Full administrative access |
| user     | user123     | User            | Standard user access     |
| readonly | readonly123 | ReadOnly        | Read-only access         |

**?? Important**: In production, replace these demo users with a proper user management system connected to a database.

### Protected Endpoints

All API endpoints are now protected and require authentication:

- ? `POST /api/aggregation/aggregate` - Requires valid JWT token
- ? `GET /api/aggregation` - Requires valid JWT token
- ? `GET /api/statistics` - Requires valid JWT token
- ? `GET /api/statistics/{apiName}` - Requires valid JWT token
- ? `DELETE /api/statistics` - Requires valid JWT token

### Public Endpoints

These endpoints do not require authentication:

- ?? `POST /api/authentication/login` - Get JWT token
- ?? `GET /api/authentication/me` - Verify token (requires valid token but accessible for testing)

## ?? Performance Monitoring Background Service

A background service continuously monitors API performance and detects anomalies in real-time.

### Features

- **Continuous Monitoring**: Runs every minute to analyze performance metrics
- **Anomaly Detection**: Automatically detects when API performance degrades
- **Time-windowed Analysis**: Compares recent performance (last 5 minutes) to overall average
- **Configurable Thresholds**: 50% performance degradation threshold by default
- **Comprehensive Logging**: Detailed logs of detected anomalies

### How It Works

1. **Metric Collection**: Every API call's response time is recorded with a timestamp
2. **Periodic Analysis**: Every minute, the service analyzes all tracked APIs
3. **Performance Comparison**: 
   - Calculates average response time over the last 5 minutes
   - Compares to overall average response time
   - Calculates percentage increase
4. **Anomaly Detection**: If recent average is 50%+ higher than overall average, logs a warning

### Example Log Output

When performance is normal:
```
[Debug] API 'Weather' performance is normal: Recent=234.50ms, Overall=245.20ms, Change=-4.4%
```

When an anomaly is detected:
```
[Warning] ?? PERFORMANCE ANOMALY DETECTED for API 'News': 
Recent average (890.25ms over last 5 minutes) is 78.3% higher 
than overall average (499.45ms). This exceeds the 50% threshold.
```

### Architecture

```
???????????????????????????????????????????????????????
?       PerformanceMonitoringService                   ?
?       (BackgroundService - runs every minute)        ?
?                                                      ?
?  1. Get all tracked APIs                            ?
?  2. For each API:                                   ?
?     - Get recent average (last 5 min)               ?
?     - Get overall average (all time)                ?
?     - Calculate percentage difference                ?
?     - Log warning if > 50% increase                 ?
???????????????????????????????????????????????????????
                         ?
                         ?
???????????????????????????????????????????????????????
?       PerformanceAnalyticsService                    ?
?  - Stores timestamped performance metrics           ?
?  - Calculates time-windowed averages                ?
?  - Tracks all API calls over time                   ?
???????????????????????????????????????????????????????
                         ?
                         ?
???????????????????????????????????????????????????????
?            StatisticsService                         ?
?  - Records every API call success/failure           ?
?  - Sends metrics to PerformanceAnalyticsService     ?
???????????????????????????????????????????????????????
```

### Customization

You can customize the monitoring behavior by modifying `PerformanceMonitoringService`:

```csharp
// In PerformanceMonitoringService.cs
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);        // How often to check
private readonly int _timeWindowMinutes = 5;                               // Time window to analyze
private readonly double _anomalyThresholdPercentage = 50.0;               // Threshold percentage
```

## ?? Configuration

### JWT Configuration (appsettings.json)

```json
{
  "Jwt": {
    "SecretKey": "ThisIsASecretKeyForJwtTokenGeneration32CharactersMinimum!",
    "Issuer": "api-aggregator",
    "Audience": "api-aggregator-clients",
    "ExpirationMinutes": 60
  }
}
```

**Configuration Options:**

| Setting | Description | Requirements |
|---------|-------------|-------------|
| `SecretKey` | Secret key for signing JWT tokens | **Minimum 32 characters** |
| `Issuer` | Token issuer identifier | Any string |
| `Audience` | Token audience identifier | Any string |
| `ExpirationMinutes` | Token lifetime in minutes | Positive integer (default: 60) |

### Environment-specific Configuration

**Development** (appsettings.Development.json):
```json
{
  "Jwt": {
    "SecretKey": "DevelopmentSecretKeyForJwtTokenGeneration32CharactersMinimum!",
    "Issuer": "api-aggregator-dev",
    "Audience": "api-aggregator-clients-dev",
    "ExpirationMinutes": 120
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "api_aggregator.Services.BackgroundServices": "Debug"
    }
  }
}
```

**Production**: Use environment variables or Azure Key Vault:
```bash
export Jwt__SecretKey="YourProductionSecretKey32CharactersMinimum!!"
export Jwt__Issuer="api-aggregator-prod"
export Jwt__Audience="api-aggregator-clients-prod"
export Jwt__ExpirationMinutes=30
```

## ?? Usage Examples

### 1. Authenticate and Get Token

**Request:**
```bash
curl -X POST https://localhost:5001/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-15T11:30:00Z",
  "username": "admin",
  "roles": ["Admin", "User"]
}
```

### 2. Use Token to Access Protected Endpoint

**Request:**
```bash
curl -X GET https://localhost:5001/api/aggregation?sources=Users \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response:**
```json
{
  "items": [...],
  "metadata": {
    "totalItems": 3,
    "successfulApis": ["Users"],
    "failedApis": [],
    "aggregatedAt": "2024-01-15T10:30:00Z"
  }
}
```

### 3. Verify Token

**Request:**
```bash
curl -X GET https://localhost:5001/api/authentication/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response:**
```json
{
  "username": "admin",
  "roles": ["Admin", "User"],
  "authenticated": true
}
```

### 4. Monitor Performance Logs

Watch the application logs for performance monitoring:

```bash
# Run the application
dotnet run --project api-aggregator.WebAPI

# Example log output:
[10:30:01 INF] Performance Monitoring Service started. Checking every 1 minutes.
[10:31:01 DBG] API 'Users' performance is normal: Recent=145.23ms, Overall=156.78ms, Change=-7.4%
[10:32:01 WRN] ?? PERFORMANCE ANOMALY DETECTED for API 'Weather': 
                Recent average (756.89ms over last 5 minutes) is 65.2% higher 
                than overall average (458.23ms). This exceeds the 50% threshold.
```

## ?? Testing

### Test Authentication

```bash
# Test with invalid credentials
curl -X POST https://localhost:5001/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "wrongpassword"}'

# Expected: 401 Unauthorized

# Test accessing protected endpoint without token
curl -X GET https://localhost:5001/api/aggregation

# Expected: 401 Unauthorized

# Test with valid token
TOKEN=$(curl -s -X POST https://localhost:5001/api/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"username": "user", "password": "user123"}' | jq -r '.token')

curl -X GET https://localhost:5001/api/aggregation?sources=Users \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 OK with data
```

### Test Performance Monitoring

1. **Run the application** and observe startup logs:
   ```
   [INF] Performance Monitoring Service started. Checking every 1 minutes.
   ```

2. **Make API calls** to generate metrics:
   ```bash
   # Make multiple calls
   for i in {1..10}; do
     curl -X GET "https://localhost:5001/api/aggregation?sources=Users" \
       -H "Authorization: Bearer $TOKEN"
     sleep 1
   done
   ```

3. **Simulate slow performance** (if possible, configure external APIs to respond slowly or use a test API)

4. **Check logs** after 1-2 minutes:
   ```
   [DBG] API 'Users' performance is normal: Recent=123.45ms, Overall=134.56ms, Change=-8.3%
   ```

5. **View anomaly detection** when performance degrades:
   ```
   [WRN] ?? PERFORMANCE ANOMALY DETECTED for API 'Weather': ...
   ```

### Unit Testing

The solution includes unit tests for the new features:

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~StatisticsServiceTests"
```

Updated tests include:
- ? StatisticsService now verifies PerformanceAnalyticsService integration
- ? Mock IPerformanceAnalyticsService in tests
- ? Verify metrics are recorded correctly

## ?? Security Best Practices

### Production Deployment

1. **Use Strong Secret Keys**
   - Minimum 32 characters
   - Use a random generator: `openssl rand -base64 32`
   - Store in Azure Key Vault or AWS Secrets Manager

2. **Short Token Expiration**
   - Development: 60-120 minutes
   - Production: 15-30 minutes
   - Implement refresh tokens for better UX

3. **HTTPS Only**
   - Always use HTTPS in production
   - Configure proper SSL certificates

4. **Replace Demo Users**
   - Connect to a real user database
   - Implement proper user registration/management
   - Add password policies (complexity, expiration)

5. **Add Rate Limiting**
   - Prevent brute force attacks on login endpoint
   - Use AspNetCoreRateLimit package

6. **Implement Refresh Tokens**
   - Allow token renewal without re-authentication
   - Store refresh tokens securely

### Example Production Configuration

```csharp
// In a production AuthenticationService, use a database:
public class ProductionAuthenticationService : IAuthenticationService
{
    private readonly UserDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public async Task<ServiceResult<LoginResponse>> AuthenticateAsync(
        string username, string password)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new ServiceResult<LoginResponse>(
                ApiErrorCode.ValidationError,
                "Invalid username or password");
        }

        return _tokenService.GenerateToken(user.Username, user.Roles);
    }
}
```

## ?? Monitoring and Observability

### Logging Levels

Configure appropriate logging levels:

**Development:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "api_aggregator.Services.BackgroundServices": "Debug"
    }
  }
}
```

**Production:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "api_aggregator.Services.BackgroundServices": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Integration with Monitoring Tools

The performance monitoring service logs can be integrated with:

- **Application Insights** (Azure)
- **CloudWatch** (AWS)
- **Elasticsearch + Kibana**
- **Splunk**
- **Datadog**

Example Application Insights configuration:
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-key-here"
  }
}
```

## ?? Next Steps

Consider implementing these enhancements:

1. **Database Integration**
   - Replace in-memory user store with Entity Framework Core
   - Add user management endpoints (register, update, delete)

2. **Advanced Authorization**
   - Implement policy-based authorization
   - Add role-based endpoint restrictions
   - Implement resource-based authorization

3. **Refresh Tokens**
   - Implement token refresh mechanism
   - Store refresh tokens in database
   - Add token revocation

4. **Performance Metrics API**
   - Expose performance analytics through API endpoints
   - Create dashboard for real-time monitoring
   - Add alerting webhooks

5. **Advanced Anomaly Detection**
   - Machine learning-based anomaly detection
   - Multiple threshold levels (warning, critical)
   - Automatic remediation actions

## ?? Additional Resources

- [JWT.io](https://jwt.io/) - JWT debugger and documentation
- [BCrypt](https://github.com/BcryptNet/bcrypt.net) - Password hashing library
- [ASP.NET Core Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Background Services in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

---

**Built with ?? using .NET 9 and ASP.NET Core**
