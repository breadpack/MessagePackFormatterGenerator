# MessagePakc Formatter Generator

MessagePack의 Custom Serialization을 위한 AOT Source Generator 입니다.

## Installation

### Nuget Package Manager

```bash
dotnet add package dev.breadpack.messagepackformattergenerator
```

### Unity Package Manager

```
https://github.com/breadpack/MessagePackFormatterGenerator.git?path=UnityPackage
```

## Usage

[MessagePackObject] 속성이 사용된 타입과 해당 타입의 멤버에 사용된 타입들을 Recursive하게 검색하면서 CustomFormatter를 생성합니다.

Type이 Blittable인 경우를 인식하여 Binary array로 빠르게 Serialzation 처리하는 Formatter를 생성합니다.

