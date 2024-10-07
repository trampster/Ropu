# Router Protocol
This is the protocol used for from clients to the router and from router to router

## Register Client
Sent by the client to register with the Router

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x00                                     |
| 1-4      | Client ID           | Unique identifier for client             |
