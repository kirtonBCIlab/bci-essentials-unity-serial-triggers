# Serial Trigger Box Support for [BCI Essentials Unity](https://docs.bci.games/bessy-unity)
A Unity package implementing the transmission of byte codes to the stim channel of an EEG amplifier alongside BCI Essentials data markers.
A Unity package for development of BCI applications. This environment needs a BCI Essentials back end *([BCI-essentials-Python](https://github.com/kirtonBCIlab/bci-essentials-python))* to work properly.

## Getting Started
**More information in [the documentation](https://docs.bci.games/bessy-unity/getting-started)**.

### Install into Unity Project
Follow these instructions to install this package into an existing project along with BCI Essentials Unity. For instructions on how to add packages hosted on Github using Unity's Package Manager [click here](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

1. Install [LSL4Unity Package](https://github.com/labstreaminglayer/LSL4Unity.git) using git URL: `https://github.com/labstreaminglayer/LSL4Unity.git`
2. Install [BCI Essentials Package](https://github.com/kirtonBCIlab/bci-essentials-unity.git) using git URL: `https://github.com/kirtonBCIlab/bci-essentials-unity.git`
3. Install [BCI Essentials Serial Triggers Package](https://github.com/kirtonBCIlab/bci-essentials-unity-serial-triggers.git) using git URL: `https://github.com/kirtonBCIlab/bci-essentials-unity-serial-triggers.git`

*Note - tested with Unity version 6000.3.9f1, some editor versions might not work.*

## Serial Port Trigger Output
BCI Essentials supports sending single-byte trigger codes over a serial port to a hardware trigger box alongside LSL markers. This is useful for synchronizing BCI events with EEG amplifiers that accept triggers via a serial/parallel stim channel.

### Setup
Add a **`SerialTriggerMarkerWriter`** component to the **BCIController** GameObject (the same object that has the `BCIController` script). This is a drop-in replacement for `MarkerWriter` that handles both LSL output and serial triggers in a single component. Because it extends `MarkerWriter`, the existing `CommunicationComponentProvider` wiring discovers it automatically. LSL output is completely unchanged; serial triggers fire alongside it.

**You must provide a mapping of event markers to byte codes by extending the abstract `SerialTriggerMarkerWriter` class.** A general-purpose "opinionated" version is provided that may be sufficient for certain experiments

Configure the serial port in the Inspector:

| Field | Description | Default |
|---|---|---|
| **Port Name** | Serial port, e.g. `COM3` (Windows) or `/dev/ttyUSB0` (Linux/macOS). Use the **Scan Ports** button to list available ports with device names. | `COM3` |

Advanced port settings (Baud Rate, Parity, Data Bits, Stop Bits, Write Timeout, Connect On Awake) are available below. Most trigger boxes work with the defaults (9600 baud, 8N1).

### How It Works
- When a BCI marker is pushed (e.g. Trial Started, P300 flash), the trigger byte is sent over the serial port, held for **Pulse Width Ms** (default 10 ms), then followed by a `0` byte to reset the trigger line. This produces a pulse of known minimum width that the EEG amplifier's stim channel can reliably detect.
- On application exit or when the serial port is disconnected, a `0` byte is sent to ensure the trigger line is reset to baseline.
- LSL marker output is unaffected.

### Status Marker Byte Mapping
Status markers use the high byte range (240-245, >= 0xf0) by default to stay well clear of stimulus values:

| Event | Default Byte |
|---|---|
| Trial Started | 240 |
| Trial Ends | 241 |
| Training Complete | 242 |
| Train Classifier | 243 |
| Update Classifier | 244 |
| Done with RS Collection | 245 |

Status bytes are configurable by overriding the `ResolveStatusMarkerTriggerCode` method of the  `SerialTriggerMarkerWriter` class but should stay above any stimulus byte values to avoid collisions.

### Event Marker Byte Mapping
As the BCI Essentials communication protocol is unable to be fully represented by a series of byte codes, an opinionated mapping of event markers onto byte codes must be provided for each experiment.

This can be done by overloading the `ResolveEventMarkerTriggerCode` method of a custom component inheriting the `SerialTriggerMarkerWriter` class.

#### Suggested Mappings
- MI / SSVEP
    - Index of training target
    - Special value for classification epochs
- P300 Single Flash
    - Stimulus presenter index
    - Leading 1 for indication of training target before trial i.e. 10000010
- P300 Multiflash
    - Bitwise flags of stimulus presenter indices *(if <= 7 presenters>)*
    - Index of first stimulus presenter

See the `OpinionatedSerialTriggerMarkerWriter` component for an example and possible solution.

#### Per-Stimulus Byte Overrides
It may also be desired to send a unique byte code for different stimulus presenters. In this case, consider sending serial triggers directly from your trial behaviour *(with codes specified by its referenced presenters)* using the underlying `SerialPortPulseWriter` class rather than through a `MarkerWriter`. A simpler mapping of stimulus indices to byte codes may also suffice.

## Considerations
As the current BCI Essentials communication protocol is incompatible with this data format, a python implementation of a BCI Essentials back end as it currently exists would need to be modified to interpret these trigger codes.

## Authorship
**This functionality was originally proposed and implemented by [Tab Memmott](https://github.com/tab-cmd)**