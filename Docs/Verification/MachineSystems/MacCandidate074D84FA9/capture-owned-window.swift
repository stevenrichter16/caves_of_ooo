import Foundation
import AppKit
import ScreenCaptureKit
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers
NSApplication.shared.setActivationPolicy(.prohibited)
_ = NSScreen.screens
let pid = Int32(CommandLine.arguments[1])!
let path = CommandLine.arguments[2]
var complete = false
var failure: String? = nil
SCShareableContent.getExcludingDesktopWindows(false, onScreenWindowsOnly: false) { content, error in
    guard let window = content?.windows.first(where: { $0.owningApplication?.processID == pid && $0.frame.width > 200 && $0.frame.height > 150 }) else {
        failure = error?.localizedDescription ?? "No owned window"; complete = true; return
    }
    let filter = SCContentFilter(desktopIndependentWindow: window)
    let config = SCStreamConfiguration()
    config.width = Int(window.frame.width)
    config.height = Int(window.frame.height)
    config.showsCursor = false
    SCScreenshotManager.captureImage(contentFilter: filter, configuration: config) { image, error in
        guard let image = image, let destination = CGImageDestinationCreateWithURL(URL(fileURLWithPath: path) as CFURL, UTType.png.identifier as CFString, 1, nil) else {
            failure = error?.localizedDescription ?? "No image"; complete = true; return
        }
        CGImageDestinationAddImage(destination, image, nil)
        if !CGImageDestinationFinalize(destination) { failure = "Image finalization failed" }
        print("Owned PID \(pid), window \(window.windowID), image \(image.width)x\(image.height)")
        complete = true
    }
}
let deadline = Date().addingTimeInterval(20)
while !complete && Date() < deadline { RunLoop.current.run(until: Date().addingTimeInterval(0.1)) }
if !complete || failure != nil { fputs((failure ?? "Capture timed out") + "\n", stderr); exit(2) }
