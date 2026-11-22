import Foundation
import AppKit
import CoreGraphics
import ApplicationServices
let pid = Int(CommandLine.arguments[1])!
let windows = CGWindowListCopyWindowInfo(.optionOnScreenOnly, kCGNullWindowID) as? [[String: Any]] ?? []
let owned = windows.filter { ($0[kCGWindowOwnerPID as String] as? Int) == pid && ($0[kCGWindowLayer as String] as? Int) == 0 }
let result: [String: Any] = ["pid": pid, "nativeInputAccess": CGPreflightPostEventAccess(), "windows": owned.map { ["id": $0[kCGWindowNumber as String] ?? 0, "bounds": $0[kCGWindowBounds as String] ?? [:]] }]
let data = try JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])
print(String(data: data, encoding: .utf8)!)
