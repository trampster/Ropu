# Router Protocol
This is the protocol used for from clients to the router and from distributors to routers

## Register Client
Sent by the client to register with the Router

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x00                                     |
| 1-4      | Client ID           | Unique identifier for client             |


## Register Client Response
Sent by the client to acknowledge a registration

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x01                                     |

## Individual Message
Request to send a packet to a client registered with the router

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x02                                     |
| 1-4      | Client ID           | Client to send packet to                 |
| 5-X      | Payload             | Payload to send to client                |

## Unknown Recipient
Response to a 'Send to Client' request when the recipient is not registered with the router.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x03                                     |
| 1-4      | Client ID           | Unique identifier for client             |

## Heartbeat
Sent by a client every 30 seconds, to keep the nat alive and to check the connection is still working.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x04                                     |

## Heartbeat Response
Response to a heartbeat packet.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x05                                     |