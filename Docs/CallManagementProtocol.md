# Call Management Protocol
This document details the protocol used for communication between the ControllingFunction and MediaController/FloorController for managing calls

## Design Principles
* Binary - to reduce packet size and increase parsing performance
* UDP - for less packet overhead, control over retries, can no connection overheads.

## Packets

### Register Media Controller
Sent to the Controlling Function to register as a media controller
* Packet Type 0 (byte)
* Request ID (uint32)
* UDP Control Port (ushort)
* UDP Media Endpoint (4 bytes IP, 2 bytes port) - this needs to be the externally facing endpoint that clients can use

### Register Floor Controller
Sent to the Controlling Function to register as a floor controller
* Packet Type 1 (byte)
* Request ID (uint32)
* UDP Port (ushort)
* Floor Control Endpoint (4 bytes IP, 2 bytes port) - this needs to be the externally facing endpoint that clients can use

### Start Call
Tells the MediaController or Floor Controller to start managing a call
* Packet Type 2 (byte)
* Request ID (uint32)
* Call ID (uint16)
* Group ID (uint16)

### Ack
Acknoledgement to one of the other message types, all messages must be acknoledged
* Packet Type 3 (byte)
* Request ID (uint32)

