## Limiting BucStop AWS Instance by IP Addresses

When hosting on AWS, it is possible to limit the access to the instance by setting specific IP addresses that are allowed to access the system.

NOTE:
	- Security groups are required on instance.
	- Specific IPs will not be provided in this document.
	- Assuming hosting on AWS instances. Options may be named differently on other services.

	
## IP Selection

Assuming that the server is not hosted locally, we will need an external IP address.

This shows a basic network diagram of why:

Client PC <---> Local Network <---> "The Web" <---> AWS <---> EC2 Instance

Since AWS is hosted on a separate network, the external IP address from the network we want to access the instance from must be used, as this is what the AWS instance "sees" when we make a connection. If this were hosted locally, we would change this to an "internal" IP, such as 192.168.XXX.XXX, 10.X.X.X, 172.XXX.XXX.XXX...


## How To Accomplish

1. Get external IP. When connected to the network (if this IP is not known) - use curl ipinfo.io
2. In AWS, search for "Security Groups". This should have been created when the instance was created if directions were followed appropriately.
3. Select the security group and "edit inbound rules".
4. There may already be entries here. The following ports should be limited: 80, 443, and 8080 - 8085. 
5. The "Source" will be the external IP from earlier.
6. Save these changes.