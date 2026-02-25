# Deployment Guide - Running LLMUnity with LM Studio

**Last Updated:** February 24, 2026

## Quick Start Deployments

### Single Machine (Recommended for Testing)
```
LM Studio Server      Game/App
(localhost:1234) ←→ (localhost:any)

Works out of the box - no configuration needed
```

### Local Network (Same Building)
```
LM Studio Server      Game/App
(192.168.1.10:1234) ←→ (192.168.1.x:any)

Configuration: Set host = "192.168.1.10"
```

### Remote/Cloud (Different Networks)
```
LM Studio Server      Game/App
(cloud.example.com:1234) ←→ (internet:any)

Configuration: Set host = "cloud.example.com"
```

---

## Scenario 1: Development Environment (Single Machine)

### Setup
```
Your Development PC
├─ LM Studio (running on localhost:1234)
└─ Unity Editor (connects to localhost:1234)
```

### Configuration
```csharp
// In LLMClient inspector or code:
client.remote = true
client.host = "localhost"
client.port = 1234
```

### Verification
```csharp
await LMStudioSetup.ValidateConnection("localhost", 1234);
// Logs: ✓ Server is online at localhost:1234
```

---

## Scenario 2: Local Network (Home/Office Network)

### Requirements
- LM Studio on Windows/Mac PC (not a laptop going to sleep)
- Game on same WiFi/Ethernet network
- Port 1234 accessible between devices

### Step-by-Step Setup

#### 2.1 Find Your Server's IP Address

**On Windows (Server Machine):**
```powershell
ipconfig
# Look for IPv4 Address like: 192.168.1.10 or 10.0.0.5
```

**On Mac (Server Machine):**
```bash
ifconfig
# Look for "inet 192.168.x.x" under en0 (WiFi) or en1
```

#### 2.2 Configure LM Studio
1. Start LM Studio
2. Go to Developer tab
3. Click "Start Server"
4. Verify it shows the correct port (1234)
5. Keep LM Studio running

#### 2.3 Configure Unity Client
```csharp
// In your game script:
void Start()
{
    LMStudioSetup.ConfigureForLMStudio(
        llmClient,
        host: "192.168.1.10",  // Replace with your server IP
        port: 1234
    );
}
```

#### 2.4 Test Connection
```csharp
// In your game script or test:
await LMStudioSetup.ValidateConnection("192.168.1.10", 1234);
```

### Troubleshooting Local Network

**"Cannot connect to server"**
```
1. Verify server IP is correct
   - Ping the server: ping 192.168.1.10
   - Should get response

2. Check port is open
   - On server: netstat -an | findstr 1234 (Windows)
   - Should show LISTENING

3. Check firewall
   - Windows Defender: Allow port 1234 inbound
   - Mac: System Preferences > Security > Firewall

4. Verify both on same network
   - Server: ipconfig (look at network)
   - Client: ipconfig (must be same network)
```

### Local Network Examples

#### Example 1: Home Gaming Setup
```
Living Room PC (LM Studio)
IP: 192.168.1.100
Port: 1234

Bedroom Switch (Running Game)
LLMClient configured:
- host: "192.168.1.100"
- port: 1234
```

#### Example 2: Office Development
```
Development PC Main (LM Studio)
IP: 10.0.0.50 (company network)

Game Dev Laptop
LLMClient configured:
- host: "10.0.0.50"
- port: 1234
```

---

## Scenario 3: Cloud/Remote Deployment

### Architecture
```
                 Internet
                    ↑↓
         ┌──────────────────────┐
         │  Cloud Provider      │
         │  (AWS/GCP/Azure)     │
         │                      │
         │  └─ LM Studio        │
         │     (GPU Instance)   │
         │     Port: 1234       │
         └──────────────────────┘
                    ↑↓
                Internet
                    ↑↓
         ┌──────────────────────┐
         │   Player's Client    │
         │   (Game)             │
         │   (Any Network)      │
         └──────────────────────┘
```

### 3.1 AWS EC2 Setup

#### Prerequisites
- AWS Account
- EC2 instance type: `g4dn.xlarge` (has GPU)
- OS: Ubuntu 22.04 or similar

#### Step 1: Launch EC2 Instance
```bash
# Create instance with:
- Machine type: g4dn.xlarge (or better)
- Storage: 100GB+ SSD
- Security group: Allow inbound on port 1234 (0.0.0.0/0)
- OS: Ubuntu 22.04 LTS
```

#### Step 2: Install LM Studio on EC2
```bash
# SSH into instance
ssh -i your-key.pem ubuntu@your-instance-ip

# Download LM Studio (Linux version)
cd /tmp
wget https://releases.lmstudio.ai/linux-latest

# Extract and run
tar -xzf lm-studio-linux.tar.gz
./lm-studio/bin/lm-studio
```

#### Step 3: Configure Client
```csharp
// In your game:
void Start()
{
    LLMUnitySetup.ConfigureForLMStudio(
        llmClient,
        host: "ec2-XXX-XXX-XXX-XXX.compute.amazonaws.com",
        port: 1234
    );
}
```

### 3.2 Google Cloud Platform

#### GCP Setup
```bash
# Create Compute Engine instance
gcloud compute instances create llm-server \
  --machine-type=g4-standard-4 \
  --accelerator=type=nvidia-tesla-t4,count=1 \
  --image-family=ubuntu-2204-lts \
  --image-project=ubuntu-os-cloud \
  --zone=us-central1-a

# Open firewall port
gcloud compute firewall-rules create allow-lm-studio \
  --allow=tcp:1234 \
  --source-ranges=0.0.0.0/0

# Install LM Studio (same as AWS)
```

### 3.3 Azure VM

#### Azure Setup
```bash
# Create VM with GPU
az vm create \
  --resource-group mygroup \
  --name llm-server \
  --image UbuntuLTS \
  --size Standard_NC4as_T4_v3 \
  --public-ip-sku Standard

# Open port 1234
az network nsg rule create \
  --resource-group mygroup \
  --nsg-name llm-serverNSG \
  --name allow-lm-studio \
  --priority 100 \
  --direction Inbound \
  --destination-port-ranges 1234
```

### Costs & Recommendations

| Provider | Instance Type | Cost/Month | GPU |
|----------|---------------|-----------|-----|
| AWS | g4dn.xlarge | $600-800 | NVIDIA T4 |
| GCP | g4-standard-4 | $500-700 | T4 |
| Azure | NC4as_T4_v3 | $400-600 | T4 |
| **Cheaper** | t2.xlarge | $150-200 | CPU only |

**Recommendation:** Start with CPU-only (`t2.xlarge`) for testing, upgrade to GPU for production.

---

## Scenario 4: Docker Containerization

### 4.1 Dockerfile for LM Studio Server

```dockerfile
FROM nvidia/cuda:12.0-runtime-ubuntu22.04

WORKDIR /app

# Install LM Studio
RUN apt-get update && apt-get install -y wget
RUN wget https://releases.lmstudio.ai/linux-latest && \
    tar -xzf lm-studio-linux.tar.gz && \
    rm lm-studio-linux.tar.gz

# Expose API port
EXPOSE 1234

# Run LM Studio server
CMD ["./lm-studio/bin/lm-studio-server"]
```

### 4.2 Docker Compose (Single Machine)

```yaml
version: '3.8'

services:
  lm-studio:
    image: nvidia/cuda:12.0-runtime-ubuntu22.04
    container_name: lm-studio-server
    ports:
      - "1234:1234"
    volumes:
      - .models:/models  # Persist downloaded models
    environment:
      - CUDA_VISIBLE_DEVICES=0
    restart: unless-stopped

  game:
    image: your-game-image:latest
    depends_on:
      - lm-studio
    environment:
      - LM_STUDIO_HOST=lm-studio
      - LM_STUDIO_PORT=1234
```

### 4.3 Running Docker

```bash
# Build
docker build -t lm-studio-server .

# Run
docker run \
  --gpus all \
  -p 1234:1234 \
  -v ~/.cache/lm-studio:/models \
  lm-studio-server

# Compose
docker-compose up -d
```

---

## Scenario 5: Kubernetes Production Deployment

### 5.1 Kubernetes Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: lm-studio-service
spec:
  selector:
    app: lm-studio
  type: LoadBalancer
  ports:
    - protocol: TCP
      port: 1234
      targetPort: 1234

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lm-studio-deployment
spec:
  replicas: 1
  selector:
    matchLabels:
      app: lm-studio
  template:
    metadata:
      labels:
        app: lm-studio
    spec:
      containers:
      - name: lm-studio
        image: lm-studio-server:latest
        ports:
        - containerPort: 1234
        resources:
          limits:
            nvidia.com/gpu: 1
        volumeMounts:
        - name: models
          mountPath: /models
      volumes:
      - name: models
        persistentVolumeClaim:
          claimName: lm-studio-pvc
```

### 5.2 Deploy to Kubernetes

```bash
# Deploy
kubectl apply -f lm-studio-k8s.yaml

# Check service
kubectl get service lm-studio-service

# Get external IP
kubectl get svc lm-studio-service -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
```

---

## Network & Firewall Configuration

### Opening Ports

#### Windows Firewall (Host LM Studio)
```powershell
# Allow inbound on port 1234
netsh advfirewall firewall add rule name="LM Studio" \
  dir=in action=allow protocol=tcp localport=1234
```

#### macOS Firewall
```bash
# System Preferences > Security & Privacy > Firewall Options
# Click "Firewall Options" and add LM Studio app
```

#### Linux UFW (Cloud Server)
```bash
sudo ufw allow 1234
sudo ufw enable
```

### Port Forwarding (for Remote NAT)

If accessing through router:
```
Router: Enable Port Forwarding
  External Port: 1234
  Internal IP: 192.168.1.10 (your LM Studio PC)
  Internal Port: 1234
```

### SSL/TLS (Recommended for Production)

```csharp
// Future: LM Studio with SSL
client.host = "https://llm.example.com";
// (configure certificate on server)
```

---

## CI/CD Integration

### GitHub Actions (Build + Deploy)

```yaml
name: Build and Deploy Game with LM Studio

on: [push]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Build Unity Game
        uses: game-ci/unity-builder@v2
        with:
          targetPlatform: StandaloneLinux64
      
      - name: Deploy to Server
        env:
          SERVER_IP: ${{ secrets.LM_STUDIO_SERVER_IP }}
          SSH_KEY: ${{ secrets.SSH_KEY }}
        run: |
          mkdir -p ~/.ssh
          echo "$SSH_KEY" > ~/.ssh/id_rsa
          chmod 600 ~/.ssh/id_rsa
          
          # Deploy game build
          scp -r build/ ubuntu@$SERVER_IP:/game/
          
          # Configure LM_STUDIO_HOST in game
          ssh ubuntu@$SERVER_IP "echo $SERVER_IP > /game/config/lm_host.txt"
```

### GitLab CI

```yaml
stages:
  - build
  - test
  - deploy

variables:
  LM_STUDIO_HOST: $CI_SERVER_HOST
  LM_STUDIO_PORT: "1234"

build:
  stage: build
  script:
    - unity -projectPath $CI_PROJECT_DIR -buildGameServer Linux64
  artifacts:
    paths:
      - build/

test:
  stage: test
  script:
    - ./build/GameServer.x86_64 --automated-test
    # Tests connect to LM_STUDIO_HOST

deploy:
  stage: deploy
  script:
    - cp -r build/* /var/www/game/
```

---

## Monitoring & Logging

### Health Check Endpoint

```csharp
// In game startup
async void CheckLMStudioHealth()
{
    bool isHealthy = await LMStudioSetup.ValidateConnection(
        client.host, 
        client.port
    );
    
    if (!isHealthy)
    {
        Debug.LogError("LM Studio unavailable - game cannot start");
        // Handle gracefully: show loading screen, retry, etc.
    }
}
```

### Logging

```bash
# LM Studio logs (Linux/Mac)
tail -f ~/.config/lm-studio/logs/server.log

# Docker logs
docker logs lm-studio-server

# Kubernetes logs
kubectl logs deployment/lm-studio-deployment
```

### Monitoring Metrics

```python
# Example: Monitor response time
import time
import requests

while True:
    start = time.time()
    response = requests.post(
        "http://lm-studio:1234/v1/completions",
        json={"prompt": "test", "max_tokens": 10}
    )
    elapsed = (time.time() - start) * 1000
    
    print(f"Response time: {elapsed:.0f}ms")
    time.sleep(60)
```

---

## Troubleshooting Deployments

### Issue: Connection Refused
```
Error: "Cannot connect to server at host:port"

Solutions:
1. Is LM Studio running? (ps aux | grep lm-studio)
2. Is server listening? (netstat -tlnp | grep 1234)
3. Is port forwarding configured?
4. Check firewall: sudo ufw status
5. Check cloud security group: ports allow inbound 1234
```

### Issue: Network Timeout
```
Error: "Request timeout after 30s"

Solutions:
1. Check network connectivity: ping server-ip
2. Is LM Studio responsive? Test with curl
3. Check latency: ping -c 5 server-ip
4. If >500ms latency, server may be overloaded
5. Consider using local mode instead
```

### Issue: Out of Memory
```
Error: "CUDA out of memory" or system freeze

Solutions:
1. Use smaller quantized model (Q4_K_M instead of FP32)
2. Reduce max_tokens parameter
3. Upgrade GPU memory (T4 → A100)
4. Use CPU-only instance and accept slower speed
```

### Issue: Connection Intermittent
```
Error: "Works sometimes, fails intermittently"

Solutions:
1. Check server logs for crashes
2. Monitor memory usage (free -h)
3. Monitor GPU usage (nvidia-smi)
4. Server may be overloaded - add retry logic
5. Consider load balancing (multiple servers)
```

---

## Production Checklist

- [ ] LM Studio running on dedicated server
- [ ] Port 1234 open in firewall
- [ ] Server has sufficient resources (GPU or fast CPU)
- [ ] Model downloaded and validated
- [ ] Network latency acceptable (<100ms)
- [ ] Error handling implemented in game
- [ ] Health checks configured
- [ ] Logging set up
- [ ] Monitoring enabled
- [ ] Backup server configured (optional)
- [ ] Documentation updated for team
- [ ] Load testing done
- [ ] Failover strategy documented

---

## References

- [LM Studio Downloads](https://lmstudio.ai)
- [AWS EC2 GPU Instances](https://aws.amazon.com/ec2/instance-types/g4)
- [Docker NVIDIA CUDA Images](https://hub.docker.com/r/nvidia/cuda)
- [Kubernetes GPU Support](https://kubernetes.io/docs/tasks/manage-gpus/)
