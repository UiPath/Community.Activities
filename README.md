Guidelines for contributing to this repository
================

### Anatomy of an Activity Pack

Each activity category follows this layout:

```
{Category}/
├── {Category}.build.props                    # Version and metadata for this pack
├── UiPath.{Category}/                        # Core library (helpers, enums)
├── UiPath.{Category}.Activities/             # Activity classes (runtime logic)
│   ├── NetCore/ViewModels/                   # ViewModel classes (design-time UI)
│   ├── Properties/                           # .resx localization files
│   └── Resources/
│       ├── Icons/                            # SVG icons
│       └── ActivitiesMetadata.json           # Links activities ↔ ViewModels
├── UiPath.{Category}.Activities.Tests/       # xUnit tests
└── UiPath.{Category}.Activities.Packaging/   # NuGet package definition
```

Every activity has three parts: **Activity** (runtime logic) → **ViewModel** (design-time UI) → **Metadata JSON** (wiring and display). Property names on the ViewModel must exactly match the Activity’s property names.

### Building and Testing

```bash
# Build the full solution
dotnet build Activities/Community.Activities.sln

# Build a specific activity pack
dotnet build Activities/Activities.Cryptography.sln

# Run tests for an activity pack
dotnet test Activities/Activities.Cryptography.sln

# Build a NuGet package
dotnet build Activities/Cryptography/UiPath.Cryptography.Activities.Packaging/UiPath.Cryptography.Activities.Packaging.csproj
```

### Development Guide

For in-depth guidance on activity code, ViewModels, widgets, rules, validation, metadata, localization, testing, and complete examples, see the [Activity Development Guide](.claude/activity-development-guide/index.md).

### Non-breaking changes

* Minor version is increased every time a change in the public interface is made (e.g. a public property is added to an activity, a new activity is added)
* Major version is increased when the package suffers major changes (e.g. some activities become obsolete, the behaviour and the interface change)
* Any new property should specify a **DefaultValue** attribute. This will decrease the potential damage for forward compatibility
* Any obsolete property should specify the **Obsolete** attribute, Browsable(false) attribute and DesignerSerializationVisibilityAttribute if its value is no longer needed. Marking the property as obsolete should not change the behaviour for any of input provided.


### Breaking changes

*(Inspired by https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/breaking-changes.md)*

To help triage breaking changes, we classify them into three buckets:

1. Public Contract
2. Reasonable Grey Area
3. Unlikely Grey Area

#### Bucket 1: Public Contract
*Clear violation of public contract.*

Examples:

* Throwing an exception in an existing common scenario where it previously was not thrown
* An exception is no longer thrown
* A different behavior is observed after the change for an input
* Renaming a public type, member, or parameter
* Decreasing the range of accepted values within a given parameter
* Changing the value of a public constant or enum member

#### Bucket 2: Reasonable Grey Area
*Change of behavior that customers would have reasonably depended on.*

Examples:

* Throwing a different exception type in an existing common scenario
* Change in timing/order of events (even when not specified in docs)
* Change in parsing of input and throwing new errors (even if parsing behavior is not specified in the docs)

These require judgment: how predictable, obvious, consistent was the behavior?

#### Bucket 3: Unlikely Grey Area
*Change of behavior that customers could have depended on, but probably wouldn’t.*

Examples:
* Correcting behavior in a subtle corner case

As with type 2 changes, these require judgment: what is reasonable and what’s not?


#### What This Means for Contributors
* All buckets (1, 2, and 3) breaking changes require talking to the repo owners first.
* If you’re not sure which bucket applies to a given change, contact us as well.
* It doesn’t matter if the old behavior is "wrong", we still need to think the implications through.
* If a change is deemed too breaking, we can help identify alternatives such as introducing a new API and deprecating the old one.
