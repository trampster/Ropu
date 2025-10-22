# Balancer Protocol
This is the protocol used for communications with the Balancer.
The Router uses this protocol to register with the Balancer and perform a heartbeat.
The Client uses this protocol to request assignment to a router.

## Register Router Packet
Sent by the Router to register with the Balancer

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x00                                     |
| 1-4      | Router IP Address   | Public IPv4 address of router            |
| 5-6      | Router Port         | Public port of router                    |
| 7-8      | Router Capacity     | Number of clients the router can support |

## Register Router Response Packet
Sent by the Balance to acknowledge Router registration

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x01                                     |
| 1-2      | Router ID           | Unique identifier (index) for router     |
|          |                     | to use.                                  |

## Router Heartbeat Packet
Sent every 5 seconds by the router. If this isn't received the Balancer should
consider the Router offline and no longer assign clients to it.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x02                                     |
| 1-2      | Router ID           | Unique identifier for router             |
| 3-4      | Registered Users    | Number of registered users               |

## Distributor Heartbeat Packet
Sent every 5 seconds by the Distributor. If this isn't received the Balancer should
consider the Distributor offline and no longer assign clients to it.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x03                                     |
| 1-2      | Distributor ID      | Unique identifier for distrubuted        |
| 3-4      | Registered Users    | Number of registered users               |

## Heartbeat Response Packet
Sent by the Balance to acknowledge a heartbeat from the router.
If the Router does not receive this packet it should consider itself unregistered. And attempt to reregister.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x04                                     |

## Router Assignment Request
Request by a Client to be assigned to a router

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x05                                     |
| 1-17     | Client ID           | ID of client (GUID)                      |

## Router Assignment
Response to a router assignement request specifying the router to use

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x06                                     |
| 1-4      | Router IP Address   | Public IPv4 address of router            |
| 5-6      | Router Port         | Public port of router                    |

## Register Distributor Packet
Sent by a Distributor to register with the Balancer

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x07                                     |
| 1-4      | Router IP Address   | Public IPv4 address of router            |
| 5-6      | Router Port         | Public port of router                    |
| 7-8      | Capacity            | Number of routers it can distribute to   | 
|          |                     | in 20 ms.                                |

## Register Distributor Response Packet
Sent by the Balancer to acknowledge Router registration

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x08                                     |
| 1-2      | Distributor ID      | Unique identifier (index) for distributor|
|          |                     | to use.                                  |

## Resolve Unit
Sent by client to get address of router which has the unit

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x09                                     |
| 1-17     | UnitId              | ID of unit to resolve (GUID)             |

## Resolve Unit Response
Sent by client to get address of router which has the unit

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x0A                                     |
| 1        | Success             | 0 = success, 1 = unit not found          |
| 2-17     | UnitId              | ID of unit (GUID)                        |
| 18-21    | Router IP Address   | Public IPv4 address of router            |
| 22-23    | Router Port         | Public port of router                    |

## Distributors List
Sent to all routers/distributor to tell them about changes to the distributor list.

| Bytes    | Field                    | Description                              |
| -------- | ------------------------ | -----------------------------------------|
| 0        | Packet Identifier        | 0x0B                                     |
| 1-2      | Sequence Number          | Increments for each change               |
| 3        | Change Type              | 0=FullList,1=Added,2=Removed,3=Changed   |
| 4-7      | distributor IP Address   | Public IPv4 address of distributor       |
| 8-9      | distributor Port         | Public port of distributor               |


## Request Distributors List
Routers/distributors should send this if they miss a sequence number.

| Bytes    | Field                    | Description                              |
| -------- | ------------------------ | -----------------------------------------|
| 0        | Packet Identifier        | 0x0C                                     |

# Router Protocol
This is the protocol used for comunication from clients to the router and from distributors to routers

## Register Client
Sent by the client to register with the Router

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x0D                                     |
| 1-17     | Client ID           | Unique identifier for client (GUID)      |


## Register Client Response
Sent by the client to acknowledge a registration

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x0E                                     |

## Individual Message
Request to send a packet to a client registered with the router

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x0F                                     |
| 1-17     | Client ID           | Client to send packet to                 |
| 18-X     | Payload             | Payload to send to client                |

## Unknown Recipient
Response to a 'Send to Client' request when the recipient is not registered with the router.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x10                                     |
| 1-17     | Client ID           | Unique identifier for client             |

## Client Heartbeat
Sent by a client every 30 seconds, to keep the nat alive and to check the connection is still working.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x11                                     |

## Client Heartbeat Response
Response to a heartbeat packet.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x12                                     |

## Group Message
Request to send a packet to a group

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x13                                     |
| 1-17     | Group ID            | Group (GUID) to send packet to           |
| 8-X      | Payload             | Payload to send to client                |

## Subscribe Groups Request
Subscribe to groups, full list, replaces existing subscriptions

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x14                                     |
| 1-17 (n) | Group ID (repeats)  | Group to subscribe to                    |

## Subscribe Groups Response
Response to a Subscribe Group request

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x15                                     |
