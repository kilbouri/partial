# Partial

A .NET library for defining partial models with the ability to distinguish between values that are default because they weren't given, and values that are default because they were set that way.

## Overview

**Partial** enables you to distinguish between properties that are unset versus properties that are set to their default values during JSON deserialization.

## Installation

To get started with Partial, simply install a serializer support library. [`Partial.Core`](./Partial.Core/README.md) will automatically be installed.

| Serializer         | Package                                                        |
| :----------------- | :------------------------------------------------------------- |
| `System.Text.Json` | [`Partial.SystemTextJson`](./Partial.SystemTextJson/README.md) |
