import Foundation
import AppKit
import CoreGraphics
import ApplicationServices
let pid = Int(CommandLine.arguments[1])!
let list = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
let windows: [[String: Any]] = list.filter { ($0[kCGWindowOwnerPID as String] as? Int) == pid }.map { row in
    return ["id": row[kCGWindowNumber as String] ?? 0, "bounds": row[kCGWindowBounds as String] ?? [:]]
}
let report: [String: Any] = ["pid":pid, "nativeInputAccess":CGPreflightPostEventAccess(), "nativeScreenAccess":CGPreflightScreenCaptureAccess(), "windows":windows]
let data = try JSONSerialization.data(withJSONObject:report, options:[.prettyPrinted,.sortedKeys])
print(String(data:data, encoding:.utf8)!)
