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

### Controller Registration Info
Sent to Controllers after they registered
tell tell them there ID and when to refresh.
* Packet Type 2 (byte)
* Request ID (uint16)
* Controller ID (byte) - to be included in the Refresh Controller packet
* Refresh Interval (ushort) - in seconds

### Refresh Controller 
* Packet Type 3 (byte)
* Request ID (byte)
* Controller ID (byte)

### Start Call
Tells the ServingNode or Call Controller a call has started
* Packet Type 4 (byte)
* Request ID (uint16)
* Call ID (uint16)
* Group ID (uint16)

### Ack
Acknoledgement to one of the other message types, all messages without specified responses must be acknowledged
* Packet Type 5 (byte)
* Request ID (uint16)

### Request Serving Node
Used by a client to request to be allocated a serving node
* Packet Type 6 (byte)
* Request ID (uint16)

### Serving Node Response
* Packet Type 7 (byte)
* Request ID (uint16)
* Serving Node Endpoint (6 bytes)

### Serving Nodes
These are automatically sent to any Serving Node as soon as it registers to tell it about 
the other serving nodes. It is also sent to all existing serving nodes whenever a serving node registeres.
* Packet Type 8 (byte)
* Request ID (uint16)
* Serving Node EndPoint(s) (6 bytes) - repeated N times

### Serving Node Removed
Sent to all registered serving nodes whenever a ServingNode deregisters or it's registration expires.
* Packet Type 9 (byte)
* Request ID (uint16)
* Serving Node Endpoint (6 bytes)

### Group Call Controllers
These are automatically sent to any serving node as soon as it registered to tell it about
what call controllers to use for each group, or whenever a group is added
* Packet Type 10 (byte)
* Request ID (uint16)
The following is repeated for each group
* Group ID (uint16)
* Serving Node EndPoint (6 bytes)

### Group Call Controller Removed
* Packet Type 11 (byte)
* Request ID (uint16)
* Group ID (uint16)


