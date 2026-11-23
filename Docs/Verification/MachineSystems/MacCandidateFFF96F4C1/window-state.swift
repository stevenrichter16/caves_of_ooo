import Foundation
import AppKit
import CoreGraphics
let pid = Int32(CommandLine.arguments[1])!
let app = NSRunningApplication(processIdentifier: pid)
let windows = (CGWindowListCopyWindowInfo(.optionAll, kCGNullWindowID) as? [[String:Any]] ?? []).filter { ($0[kCGWindowOwnerPID as String] as? Int) == Int(pid) }
let out: [String:Any] = ["pid":Int(pid), "found":app != nil, "hidden":app?.isHidden ?? false, "active":app?.isActive ?? false, "terminated":app?.isTerminated ?? true, "windows":windows]
print(String(data:try JSONSerialization.data(withJSONObject:out,options:[.prettyPrinted,.sortedKeys]),encoding:.utf8)!)
