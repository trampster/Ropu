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

## Heartbeat Packet
Sent every 5 seconds by the router. If this isn't received the Balancer should
consider the Router offline and no longer assign clients to it.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x02                                     |
| 1-2      | Router ID           | Unique identifier for router             |
| 3-4      | Registered Users    | Number of registered users               |

## Heartbeat Response Packet
Sent by the Balance to acknowledge a heartbeat from the router.
If the Router does not receive this packet it should consider itself unregistered. And attempt to reregister.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x03                                     |

## Router Assignment Request
Request by a Client to be assigned to a router

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x04                                     |

## Router Assignment
Response to a router assignement request specifying the router to use

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x05                                     |
| 1-4      | Router IP Address   | Public IPv4 address of router            |
| 5-6      | Router Port         | Public port of router                    |

## Routers Info Page
A page of registered routers. There are 2000 possible routers each with an ID
which is an index in the routers list. This is divided into 10 pages:
page 1 - indexes 0-199
page 2 - indexes 200-399
page 3 - indexes 400-599
page 4 - indexes 600-799 
page 5 - indexes 800-999
page 6 - indexes 1000-1199
page 7 - indexes 1200-1399
page 8 - indexes 1400-1599
page 9 - indexes 1600-1799 
page 10 - indexes 1800-1999 

A router should request these when they first connect and periodically afterwards to allows for any missed updates.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x06                                     |
| 1        | Page Number         | Page Number (1 - 10)                     |
The following can be repeated for each router upto 200 times.               |
| 2-4      | Router ID (index)   | The ID (index) of the router             |
| 5-8      | Router IP Address   | Public IPv4 address of router            |
| 9-10     | Router Port         | Public port of router                    |

## Router Info Page Request
Requests a router info page from the Balancer.

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x07                                     |
| 1        | Page Number         | Page Number (1 - 10)                     |


## Router Added
Packet which is sent to routers to inform them that a router has been added

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x08                                     |
| 1-2      | Router ID (index)   | The ID (index) of the router             |
| 3-6      | Router IP Address   | Public IPv4 address of router            |
| 7-8      | Router Port         | Public port of router                    |

## Router Removed
Packet which is sent to routers to inform them that a router has been  removed

| Bytes    | Field               | Description                              |
| -------- | ------------------- | -----------------------------------------|
| 0        | Packet Identifier   | 0x09                                     |
| 1-2      | Router ID (index)   | The ID (index) of the router             |