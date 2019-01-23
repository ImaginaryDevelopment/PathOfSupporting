/// Should hold any semi-global or widely used DTOs or domain object shapes
module PathOfSupporting.Schema

module Apis =
    type FetchArguments<'t> =
        | OverrideUrl of string
        | Values of 't

