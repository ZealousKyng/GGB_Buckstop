# AWS Deployment Script

This script configures and deploys your BucStop application on AWS Amazon Linux instances.

## Script Available

### `deploy.sh` (Unified Script)
**One-stop deployment script** that handles everything:
- Auto-detects instance IP address
- Updates all configuration files
- Cleans up Docker resources
- Deploys all microservices
- Creates snapshots and backups

## What the Script Does

1. **Auto-detects IP**: Automatically detects the instance's public IP address
2. **Updates Configuration**: Modifies docker-compose.yml and all appsettings.containers.json files
3. **Creates Backups**: All modified files are backed up with `.backup` extension
4. **Cleans Up**: Removes unused Docker resources to save AWS storage costs
5. **Creates Snapshots**: Preserves game data before cleanup
6. **Deploys Services**: Launches all microservices with proper configuration

## Usage

### Simple Deployment
```bash
# Auto-detect IP and deploy everything
./Scripts/deploy.sh

# Or specify IP manually
./Scripts/deploy.sh 3.232.16.65
```

## Files Modified

The script will modify these files:

1. `docker-compose.yml` - Changes environment variable
2. `Team-3-BucStop_Snake/Snake/appsettings.containers.json`
3. `Team-3-BucStop_Pong/Pong/appsettings.containers.json`
4. `Team-3-BucStop_Tetris/Tetris/appsettings.containers.json`
5. `Team-3-BucStop_BucKart/BucKart/appsettings.containers.json`
6. `Team-3-BucStop_APIGateway/APIGateway/appsettings.containers.json`
7. `Bucstop WebApp/BucStop/appsettings.containers.json` (if applicable)

## Port Mappings

After running the script, your services will be accessible at:

- Main WebApp: `http://YOUR_IP:8080`
- API Gateway: `http://YOUR_IP:8081`
- Snake Game: `http://YOUR_IP:8082`
- Pong Game: `http://YOUR_IP:8083`
- Tetris Game: `http://YOUR_IP:8084`
- BucKart Game: `http://YOUR_IP:8085`

## After Running the Script

1. Start your services:
   ```bash
   docker-compose up -d
   ```

2. Verify services are running:
   ```bash
   docker-compose ps
   ```

3. Test access to your services using the URLs above

## Backup and Recovery

All modified files are automatically backed up with `.backup` extension. To restore:

```bash
# Restore docker-compose.yml
mv docker-compose.yml.backup docker-compose.yml

# Restore appsettings files
mv Team-3-BucStop_Snake/Snake/appsettings.containers.json.backup Team-3-BucStop_Snake/Snake/appsettings.containers.json
# ... repeat for other files
```

## Troubleshooting

- If the script can't auto-detect your IP, provide it manually
- Ensure you have the necessary permissions to modify files
- Check that all required files exist in the expected locations
- Verify your AWS security groups allow traffic on ports 8080-8084
