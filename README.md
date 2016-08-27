# CS-ConfigurationSectionGenerator
Code library and tool for generating configuration sections in .NET

## Usage
```batch
ConfigurationGenerator.exe <templatefile> <destinationfile>
```

## Examples
### Simple configration with several elements
```XML
<SimpleConfigurationSection>
    <Font color="#000cd1" family="Times new roman" background="#FF0000" />
    <BackgroundImage src="images/bg.jpg" />
</SimpleConfigurationSection>
```
As you can see in this example you can add regular xml elements to the root element. 

### Collections
It isn't possible to have a collection of items in the configuration section root element

```Xml
<InvalidConfigurationSection>
    <Task time="13:04" id="12" />
    <Task time="15:00" id="33" />
    <Task time="01:00" id="8" />
</InvalidConfigurationSection>
```
Trying to generate code from this XML will result in the application failing. You should instead use the following XML
```XML
<ValidConfigurationSection>
    <Tasks>
        <Task time="13:04" id="12" />
        <Task time="15:00" id="33" />
        <Task time="01:00" id="8" />
    </Tasks>
</ValidConfigurationSection>
```

## Known issues / limitations
- Validation hasn't been implemented yet
- All classes outputted into the same file
- No way of defining attribute types (everything is a string)
- Cannot have a ConfigurationSection which has '.' in it's name
- Cannot configure the namespace for the generated classes
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