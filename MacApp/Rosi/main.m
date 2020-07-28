//
//  main.m
//
//  Created by Marko B. Ludolph on 24.07.20.
//  Copyright © 2020 Marko B. Ludolph. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "AppDelegate.h"

int main(int argc, const char * argv[]) {
    @autoreleasepool {
        // Setup code that might create autoreleased objects goes here.
    }
    AppDelegate *appDelegate = [AppDelegate new];
    NSApplication.sharedApplication.delegate = appDelegate;
    return NSApplicationMain(argc, argv);
}
