//
//  AppDelegate.m
//
//  Created by Marko B. Ludolph on 24.07.20.
//  Copyright © 2020 Marko B. Ludolph. All rights reserved.
//

#import "AppDelegate.h"

@interface AppDelegate ()

@property(nonatomic, assign) bool showWelcome;

@end

@implementation AppDelegate

- (instancetype)init
{
    _showWelcome = true;
    return [super init];
}

- (void)runRosi:(NSString *) script {

    NSString *command = [NSString stringWithFormat:@"do script \"'%@/rosi.bundle/rosi' '%@'; exit;\"", NSBundle.mainBundle.resourcePath, script];

    NSTask *task = [[NSTask alloc] init];
    [task setLaunchPath:@"/usr/bin/osascript"];
    [task setArguments:@[ @"-e", @"tell application \"Terminal\"", @"-e", @"activate", @"-e", command, @"-e", @"end tell"]];
    
    [task launch];
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification {
    
    if (_showWelcome)
    {
        [self runRosi:@"--rosimacoswelcome"];
    }
    
    [NSApplication.sharedApplication terminate:nil];
}

- (BOOL)application:(NSApplication *)sender openFile:(NSString *)filename
{
    _showWelcome = false;
    [self runRosi: filename];
    return YES;
}

@end
