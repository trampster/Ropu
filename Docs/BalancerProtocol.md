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
Sent every 5 seconds by the router. If this isn't received the Balancer should
consider the Router offline and no longer assign clients to it.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x03                                     |
| 1-2      | Router ID           | Unique identifier for router             |
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
| 1-5      | Client ID           | ID of client

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
| 1-4      | UnitId              | ID of unit to resolve                    |

## Resolve Unit Response
Sent by client to get address of router which has the unit

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x0A                                     |
| 1        | Success             | 0 = success, 1 = unit not found          |
| 2-5      | UnitId              | ID of unit                               |
| 6-9      | Router IP Address   | Public IPv4 address of router            |
| 10-11    | Router Port         | Public port of router                    |
