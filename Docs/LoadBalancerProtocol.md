# Load Balancer Protocol
This document details the protocol used for communication between the Load Balancer and the other parts of the system

## Design Principles
* Binary - to reduce packet size and increase parsing performance
* UDP - for less packet overhead, control over retries, and no connection overheads.

## Packets

### Register Serving Node
Sent to the Controlling Function to register as a Serving Node
* Packet Type 0 (byte)
* Request ID (uint16)
* Serving Node Endpoint (4 bytes IP, 2 bytes port) - this needs to be the externally facing endpoint that clients can use

### Register Call Controller
Sent to the Controlling Function to register as a floor controller
* Packet Type 1 (byte)
* Request ID (uint16)
* Call Control Endpoint (4 bytes IP, 2 bytes port) - this needs to be the externally facing endpoint that clients can use

### Start Call
Tells the MediaController or Floor Controller to start managing a call
* Packet Type 2 (byte)
* Request ID (uint16)
* Call ID (uint16)
* Group ID (uint16)

### Ack
Acknoledgement to one of the other message types, all messages without specified responses must be acknowledged
* Packet Type 3 (byte)
* Request ID (uint16)

### Registration Update
Could be a new registration or an update to an existing one
* Packet Type 4 (byte)
* Request ID (uint16)
* Group ID (uint16)
* User ID (uint32)
* EndPoint (6 bytes)

### Registration Removed
* Packet Type 5 (byte)
* Request ID (uint16)
* Group ID (uint16)
* User ID (uint32)

### Request Serving Node
Used by a client to request to be allocated a serving node
* Packet Type 6 (byte)
* Request ID (uint16)

### Serving Node Response
* Packet Type 7 (byte)
* Request ID (uint16)
* Serving Node Endpoint (6 bytes)

### Serving Nodes
These is automatically sent to any Serving Node as soon as it registers to tell it about 
the other serving nodes. It is also sent to all existing serving nodes whenever a serving node registeres.
* Packet Type 9 (byte)
* Request ID (uint16)
* Serving Node Endpoint (6 bytes) - repeated N times

### Serving Node Removed
Sent to all registered serving nodes whenever a ServingNode deregisters or it's registration expires.
* Packet Type 10 (byte)
* Request Id (uint16)
* Serving Node Endpoint (6 bytes)



