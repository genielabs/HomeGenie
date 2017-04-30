## Interconnections

### When to use more than just one HomeGenie server?

There are a couple of scenarios where interconnections may become useful. 
For instance, if we placed the main HG server in the basement but we would like to control it using an IR remote from the living room. 
Then we could setup another HG box in the living room that has just an IR receiver connected to it and nothing else. 
After that, all we have to do is configure the living room HG, so that it will forward all received IR signals to the main HG box in the basement. 
The basement HG box, will see and receive all IR signals just as if the IR receiver was physically connected to it. 
This is done from Configure->Automation->Interconnection section as shown in the picture below.

[IMAGE CONFIGURING INTERCONNECTIONS]

Now, let's say that the HG in the basement has a Z-Wave controller connected to it, while the living room HG has an X10 controller connected. By activating the "Status.Level event forwarding" from the living room HG (source node) to the basement one (target node), the basement HG will automatically inherit all X10 devices and see them as if these were effectively connected to it. 
The devices available from the source HG node will be visible on the target HG node once that an event is generated from the source HG node (eg. turn a device on/off). 
So, now, by accessing the basement HG we will be able to program and control both X10 and Z-Wave.

More in general, by interconnecting more HomeGenie boxes we can:

- Specialize each HG server to handle different hardware or services, but make them all "talk" each other from across the network
- Balance and distribute all automation tasks between HG servers and still see the whole as a single entity
- Design our home automation network as centralized or make it behave like a grid network, or a mix of both

