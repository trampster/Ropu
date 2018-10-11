# High Performance PTToC Proposal

## Problem statement
The performance and scalability of the existing design is questionable with the following potential issues identified:

### Slow Group Call Setup Time
The group call setup is slow it takes seconds to send out all the invites to a few thousand units and will be even longer if we wait for replies. Our LMR clients are used to almost instant group calls and having to wait will not be a positive user experience for them.

### Max units in a call
We have yet to test this but we need to be able to stream media to thousands of units. It is therefore not clear if a single Conference Controller will be able to meet this requirement.

### Design issues with SIP/MCPTT for scaling
*SIP*
SIP although an industry standard is horribly inefficient. It is a text based protocol which was not designed with any consideration for performance of ether parsing, writing or network usage. The invites are so bloated that with the addition of the bits required for MCPTT they no longer fit in standard UDP packets and need either fragmentation support or the use of TCP.

When a group call is created each and every participant needs to be invited and have media and ports negotiated. This is what causes the large delays in group call setup.

*RTP*
RTP has unique identifiers for each stream, this means that a Conference Controller can’t send the same packet to all participants in a call, each packet needs to be specifically created for that participant, this requires additional CPU and memory allocations which would otherwise not be required. Without this the Conference Control could take the incoming packet from the user who has the floor and without modification send it to all call participants. Such a system would be limited only by the bandwidth of the connection it is using.

## Proposed solution
Controller Protocol
Replace both the SIP and Floor Control protocols with a single control plane protocol, With the following properties:
Binary rather than text (for size and speed of parsing)
No variable length fields for messages that will be sent to all participants  (for speed of parsing)
No unique identifiers, (the same packet can be sent to all participants without modification)
Media and ports will be negotiated at registration time, not all call setup.
Presence will be keeped up to date via control plane messages so that we already know who can receive a call when it starts.
When group call starts, media will immediately be streamed to all participant, control plane messages identifying the group and talker will be sent concurrently but clients should not wait for them to unmute.
The floor will be requested by sending media.
The exact same floor granted message will be sent to all participants including the requester.
Floor denied will be sent only to the requester.

The client will share a port for the control, media and floor packets. This is so we only need to keep one mapping alive through the NAT rather than three. For this reason the packet type field needs to be unique accross all three protocols.

### Controlling Function Protocol
#### Registration
* Packet Type 0 (byte)
* User ID (uint32)
* IPAddress (4 bytes)
* Port (uint16) - control, media and floor control packets will be sent to this port

#### Registration Response
* Packet Type 1 (byte)
* User ID (uint32)
* Codec (byte) (defined via an enum, this is the codec/bitrate used by the system, you must support it, this is required so the server doesn’t have to transcode, which is an expensive operation)
* Bitrate (uint16)

#### Call Ended (units receiving this packet should not stop playing out media they receive, this should only be used to update the UI)
* Packet Type 2
* Group ID (uint16)
* Call ID (uint16)

#### Start Group Call
* Packet Type 3
* User ID (uint32)
* Group ID ((uint16))

#### Call Started (IPv4)
* Packet Type 4
* User Id (uint32)
* Group ID (uint16)
* Call ID (uint16) unique identifier for the call, to be included in the media stream
* Media Endpoint (4 bytes IP Address, 2 bytes port)
* Floor Control Endpoint (4 bytes IP Address, 2 bytes port)

### Call Start Failed
* Packet Type 5
* Reason (byte) 0 = insufficient resources, 255 = other reason

### Floor Control Protocol
#### Floor Denied
* Packet Type 6
* User ID (uint32)

#### Floor Granted
* Packet Type 7
* User ID (uint32) 
* Group ID (uint16)
* Call ID (uint16)

#### Floor Released
* Packet Type 8
* Group ID (uint16)
* Call ID (uint16)

### Media Plane Protocol
Clients should play out everything they receive on the Media Plane, regardless of the call state. This way if the control messages and media packets arrive unsynchronized the client won't miss audio.
Likewise when starting a call, the client should start streaming the media to the server as soon as they have received the Start Call Response before receiving floor granted. The call initiator is implicitly granted the floor.

#### Packet
* Packet Type 9
* Length (uint16)
* Call ID (uint16)
* Key ID (uint16)
* Payload (Length bytes)