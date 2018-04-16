Guidelines for contribuing to this repository
================

### Anatomy of an Activity pack

   * API
   * API.Activities
   * API.Activities.Design
  
    API should be the name of the service this pack integrates with (e.g. Excel, Sharepoint, Mail)
    API is not necessary if the activities use standard .NET types
    API.Activities.Design is not necessary if designers do not exist but design specific attributes should be placed in a separate file (DesignerMetadata.cs)


### Assembly and Package Info

   * GlobalAssemblyInfo.cs should be used
   * Public namespaces should specify an **XmlnsDefinitionAttribute** that is usually **http://schemas.company.com/workflow/activities**
   * NuSpec file should have the approximately same structure as the others

   
### Testing and deploying

  * To pack the packages run nuget.exe with the desired project

### Non-breaking changes:

* Minor version is increased every time a change in the public interface is made (e.g. a public property is added to an activity, a new activity is added)
* Major version is increased when the package suffers major changes (e.g. some activities become obsolete, the behaviour and the interface change)
* Any new property should specify a **DefaultValue** attribute. This will decrease the potential damage for forward compatibility
* Any obsolete property should specify the **Obsolete** attribute, Browsable(false) attribute and  DesignerSerializationVisibilityAttribute if its value is no longer needed. Marking the property as obsolete should not change the behaviour for any of input provided.


### Breaking changes:

*inspired from https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md*

To help triage breaking changes, we classify them into three buckets:

1. Public Contract
2. Reasonable Grey Area
3. Unlikely Grey Area

#### Bucket 1: Public Contract
*Clear violation of public contract.*

Examples:

* throwing an exception in an existing common scenario where it previously was not thrown
* An exception is no longer thrown
* A different behavior is observed after the change for an input
* renaming a public type, member, or parameter
* decreasing the range of accepted values within a given parameter
* changing the value of a public constant or enum member

#### Bucket 2: Reasonable Grey Area
*Change of behavior that customers would have reasonably depended on.*

Examples:

* throwing a different exception type in an existing common scenario
* change in timing/order of events (even when not specified in docs)
* change in parsing of input and throwing new errors (even if parsing behavior is not specified in the docs)

These require judgment: how predictable, obvious, consistent was the behavior?

#### Bucket 3: Unlikely Grey Area
*Change of behavior that customers could have depended on, but probably wouldn't.*

Examples:
* correcting behavior in a subtle corner case

As with type 2 changes, these require judgment: what is reasonable and what’s not?


#### What This Means for Contributors
* All bucket 1, 2, and 3 breaking changes require talking to the repo owners first.
* If you're not sure in which bucket applies to a given change, contact us as well.
* It doesn't matter if the old behavior is "wrong", we still need to think through the implications.
* If a change is deemed too breaking, we can help identify alternatives such as introducing a new API and obsoleting the old one.
