# SSRF Demo Project

## Purpose 

This repository is used to demonstrate a security vulnerability type: SSRF (Sever Side Request Forgery). 

## Business Scenario

This project is an API service for an imaginary AI RAG tool. The AI RAG tool allows user to import external contents so that the tool can answer questions based on the content that has been stored. 

The user can call `POST /import-external-content-from-url` to import a PDF from a URL. If the fetched content is PDF, in reality the API should save it somewhere. In this example, just for the demo purpose, the API will return a 200 OK message. If the content from the URL is not PDF, the application will return an error message with the fetched content for debugging purpose. 

## Tech Stack

- .NET 9 
- FastEndpoints

## API Specification

Here are two APIs in this service. 

1. `GET /` Hello world API (anonymous)
2. `POST /import-external-content-from-url` (anonymous)

For POST import API, there must be a `Content-Type: application/json` header and the payload must have a url. 

Example cURL command for happy path: 

```shell
curl -X POST --location "http://localhost:5077/import-external-content-from-url" \
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
curl -X POST --location "http://localhost:5077/import-external-content-from-url" \
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


