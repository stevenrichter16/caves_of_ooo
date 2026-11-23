import Foundation
import CoreGraphics
import ApplicationServices
let pid = CommandLine.arguments.count > 1 ? Int32(CommandLine.arguments[1])! : 0
let all = CGWindowListCopyWindowInfo([.optionAll, .excludeDesktopElements], kCGNullWindowID) as? [[String:Any]] ?? []
let windows = all.filter { ($0[kCGWindowOwnerPID as String] as? Int32) == pid }.map { ["id": $0[kCGWindowNumber as String] ?? 0, "bounds": $0[kCGWindowBounds as String] ?? [:]] }
let result: [String:Any] = ["pid": pid, "nativeInputAccess": CGPreflightPostEventAccess(), "nativeScreenAccess": CGPreflightScreenCaptureAccess(), "accessibilityTrusted": AXIsProcessTrusted(), "windows": windows]
let data = try! JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])
print(String(data:data,encoding:.utf8)!)
