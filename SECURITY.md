# Security Policy

## Supported Versions

Currently supported versions for security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it by:

1. **DO NOT** open a public issue
2. Email the maintainers privately (create a private security advisory on GitHub)
3. Include as much detail as possible:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

## Security Considerations

### API Key Protection
- Never commit API keys to version control
- Use strong, unique API keys in production
- Rotate API keys regularly
- Store API keys in secure configuration (environment variables, Azure Key Vault, etc.)

### File System Access
- This API can read and modify files on the server
- Ensure proper file system permissions
- Validate all file paths to prevent directory traversal attacks
- Consider running the service with minimal required permissions

### Network Security
- Always use HTTPS in production
- Consider implementing rate limiting
- Use proper firewall configurations
- Restrict access to trusted networks when possible

### Input Validation
- All file paths are validated
- Line ranges are bounds-checked
- Error messages don't expose sensitive system information

## Best Practices

1. **Development Environment**
   - Use different API keys for development and production
   - Keep development keys simple but unique

2. **Production Environment**
   - Use environment variables for configuration
   - Enable HTTPS only
   - Implement proper logging and monitoring
   - Regular security updates

3. **n8n Integration**
   - Store API keys in n8n's credential system
   - Use HTTPS for all API calls
   - Implement proper error handling in workflows
