# ILL status api

Requesting statuses:
```
https://{url}/umbraco/surface/PublicDataSurface/GetChillinDataForSierraPatron?recordId={userid}[&lang=sv]
```
{url} url to server  
{userid} is the id of the user in the library system,  
lang is default english but can be specified to sv for response in swedish.

If userid exists but the person has no requests to display the Items array is empty.  
If userid does not exists it still gives a successfull response but with an empty Items array.

JSON Model:
```
Response {
  Success(boolean),
  Message (string),
  Items (Array[Items], optional)
}

Items {
    Type(string),
    Reference(string),
    Status(string)
}
```

English response example:
```
{
  "Success":true,
  "Message":"Successfully fetched data.",
  "Items":[
    {
      "Type":"Unknown",
      "Reference":"Lari testar",
      "Status":"Not available yet"}
  ]
}
```

Swedish response example:
```
{
  "Success":true,
  "Message":"Successfully fetched data.",
  "Items":[
    {
      "Type":"Okänd",
      "Reference":"Lari testar",
      "Status":"Ej tillgänglig än"}
  ]
}
```