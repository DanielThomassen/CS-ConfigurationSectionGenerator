# CS-ConfigurationSectionGenerator
Code library and tool for generating configuration sections in .NET

## Usage
```batch
ConfigurationGenerator.exe <templatefile> <destinationfile>
```

Known issues / limitations:
- Validation hasn't been implemented yet
- All classes outputted into the same file
- No way of defining attribute types (everything is a string)
- Cannot have elements with the same name within the structure e.g.
```XML
<MySection>
    <WorkItems>
        <Item task="clean"  />
    </WorkItems>
    <Inventory>
        <Item name="box" />
    </Inventory>
</MySection>
```